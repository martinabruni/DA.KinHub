using System.Text.Json;
using FluentValidation.Results;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Domain.KinListFeature;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListAudioService : IKinListAudioService, IAudioOperationProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IKinListRepository _listRepository;
    private readonly IKinListItemRepository _itemRepository;
    private readonly IAudioProcessingOperationRepository _audioOperationRepository;
    private readonly IKinListAudioDraftGenerator _audioDraftGenerator;
    private readonly IAudioProcessingBlobStorage _blobStorage;
    private readonly IAudioProcessingQueuePublisher _audioQueue;
    private readonly IKinListItemDeduplicator _deduplicator;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly CreateAudioProcessingOperationBusinessValidator _createAudioOperationValidator;
    private readonly ILogger<KinListAudioService> _logger;
    private readonly KinListOptions _options;

    public KinListAudioService(
        IKinListRepository listRepository,
        IKinListItemRepository itemRepository,
        IAudioProcessingOperationRepository audioOperationRepository,
        IKinListAudioDraftGenerator audioDraftGenerator,
        IAudioProcessingBlobStorage blobStorage,
        IAudioProcessingQueuePublisher audioQueue,
        IKinListItemDeduplicator deduplicator,
        ICorrelationIdProvider correlationIdProvider,
        CreateAudioProcessingOperationBusinessValidator createAudioOperationValidator,
        ILogger<KinListAudioService> logger,
        KinListOptions options)
    {
        _listRepository = listRepository;
        _itemRepository = itemRepository;
        _audioOperationRepository = audioOperationRepository;
        _audioDraftGenerator = audioDraftGenerator;
        _blobStorage = blobStorage;
        _audioQueue = audioQueue;
        _deduplicator = deduplicator;
        _correlationIdProvider = correlationIdProvider;
        _createAudioOperationValidator = createAudioOperationValidator;
        _logger = logger;
        _options = options;
    }

    public async Task<Result<CreateAudioProcessingOperationResponse>> CreateAudioOperationAsync(CreateAudioProcessingOperationRequest request, Guid familyId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var activity = KinListAudioTelemetry.ActivitySource.StartActivity("kinlist.audio.operation.create");
        activity?.SetTag("kinlist.audio.operation.type", request.Type);
        activity?.SetTag("kinlist.audio.content_type", request.ContentType);
        activity?.SetTag("kinlist.audio.declared_bytes", request.DeclaredByteSize);

        var validation = await _createAudioOperationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationResult<CreateAudioProcessingOperationResponse>(validation);
        }

        _ = TryParseOperationType(request.Type, out var operationType);
        var normalizedMimeType = NormalizeMimeType(request.ContentType);

        if (operationType is AudioProcessingOperationType.AppendItems
            && request.ListId is Guid listId)
        {
            var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
            if (list is null || list.IsDeleted)
            {
                return Result<CreateAudioProcessingOperationResponse>.NotFound("List not found.");
            }

            if (list.FamilyId != familyId)
            {
                return Result<CreateAudioProcessingOperationResponse>.Unauthorized("The authenticated family cannot access this list.");
            }
        }

        var operationId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var blobName = $"{familyId:D}/{operationId:D}";
        var uploadTarget = await _blobStorage.CreateUploadTargetAsync(
            blobName,
            normalizedMimeType,
            TimeSpan.FromMinutes(_options.AudioUploadSasTtlMinutes),
            cancellationToken);

        var operation = new AudioProcessingOperation
        {
            Id = operationId,
            FamilyId = familyId,
            UserId = userId,
            Type = operationType,
            Status = AudioProcessingOperationStatus.AwaitingUpload,
            BlobName = uploadTarget.BlobName,
            ContentType = normalizedMimeType,
            DeclaredByteSize = request.DeclaredByteSize,
            CorrelationId = _correlationIdProvider.Resolve(),
            AttemptCount = 0,
            ExpiresAt = now.AddHours(_options.AudioOperationRetentionHours),
            ListId = request.ListId,
            CreatedAt = now,
            UpdatedAt = now,
            Version = Guid.NewGuid(),
        };

        operation = await _audioOperationRepository.AddAsync(operation, cancellationToken);

        return Result<CreateAudioProcessingOperationResponse>.Success(new CreateAudioProcessingOperationResponse
        {
            Id = operation.Id,
            UploadUrl = uploadTarget.UploadUrl,
            UploadExpiresAt = uploadTarget.ExpiresAt,
            BlobName = uploadTarget.BlobName,
            RetryAfterSeconds = _options.AudioPollingRetryAfterSeconds,
        });
    }

    public async Task<Result<AudioProcessingOperationResponse>> CompleteAudioOperationUploadAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var operation = await _audioOperationRepository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
        {
            return Result<AudioProcessingOperationResponse>.NotFound("Audio operation not found.");
        }

        if (operation.FamilyId != familyId)
        {
            return Result<AudioProcessingOperationResponse>.Unauthorized("The authenticated family cannot access this audio operation.");
        }

        if (operation.Status is AudioProcessingOperationStatus.Queued or AudioProcessingOperationStatus.Processing or AudioProcessingOperationStatus.Succeeded)
        {
            return Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(operation, cancellationToken));
        }

        var blob = await _blobStorage.GetBlobAsync(operation.BlobName, cancellationToken);
        if (blob is null)
        {
            return Result<AudioProcessingOperationResponse>.ValidationError("Uploaded audio blob was not found.", "audio_blob_missing");
        }

        if (blob.ContentLength <= 0 || blob.ContentLength > _options.MaxAudioBytes)
        {
            return Result<AudioProcessingOperationResponse>.ValidationError("Uploaded audio size is outside the configured limits.", "invalid_audio_size");
        }

        operation.Status = AudioProcessingOperationStatus.Queued;
        operation.CorrelationId = _correlationIdProvider.Resolve(operation.CorrelationId);
        operation.UploadedByteSize = blob.ContentLength;
        operation.UploadCompletedAt = DateTime.UtcNow;
        operation.UpdatedAt = DateTime.UtcNow;
        operation.Version = Guid.NewGuid();
        operation = await _audioOperationRepository.UpdateAsync(operation, cancellationToken);
        await _audioQueue.EnqueueAsync(operation.Id, operation.CorrelationId, cancellationToken);

        return Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(operation, cancellationToken));
    }

    public async Task<Result<AudioProcessingOperationResponse>> GetAudioOperationAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var operation = await _audioOperationRepository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
        {
            return Result<AudioProcessingOperationResponse>.NotFound("Audio operation not found.");
        }

        if (operation.FamilyId != familyId)
        {
            return Result<AudioProcessingOperationResponse>.Unauthorized("The authenticated family cannot access this audio operation.");
        }

        if (operation.ExpiresAt <= DateTime.UtcNow && operation.Status is not AudioProcessingOperationStatus.Succeeded and not AudioProcessingOperationStatus.Failed and not AudioProcessingOperationStatus.Cancelled)
        {
            operation.Status = AudioProcessingOperationStatus.Expired;
            operation.UpdatedAt = DateTime.UtcNow;
            operation.Version = Guid.NewGuid();
            operation = await _audioOperationRepository.UpdateAsync(operation, cancellationToken);
        }

        return Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(operation, cancellationToken));
    }

    public async Task<Result<bool>> DeleteAudioOperationAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var operation = await _audioOperationRepository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
        {
            return Result<bool>.NotFound("Audio operation not found.");
        }

        if (operation.FamilyId != familyId)
        {
            return Result<bool>.Unauthorized("The authenticated family cannot access this audio operation.");
        }

        await _blobStorage.DeleteIfExistsAsync(operation.BlobName, cancellationToken);
        operation.Status = AudioProcessingOperationStatus.Cancelled;
        operation.ErrorCode = null;
        operation.ErrorMessage = null;
        operation.UpdatedAt = DateTime.UtcNow;
        operation.CompletedAt = DateTime.UtcNow;
        operation.Version = Guid.NewGuid();
        await _audioOperationRepository.UpdateAsync(operation, cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<AudioProcessingOperationResponse>> ProcessAudioOperationAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var operation = await _audioOperationRepository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
        {
            return Result<AudioProcessingOperationResponse>.NotFound("Audio operation not found.");
        }

        var preconditionResult = await ValidateOperationPreconditionsAsync(operation, cancellationToken);
        if (preconditionResult is not null)
        {
            return preconditionResult;
        }

        // Atomically claim the operation so concurrent workers cannot process the same audio twice.
        var (claimed, claimFailure) = await ClaimOperationAsync(operation, cancellationToken);
        if (claimFailure is not null)
        {
            return claimFailure;
        }

        return await ExecuteAudioOperationAsync(claimed!, cancellationToken);
    }

    private async Task<Result<AudioProcessingOperationResponse>?> ValidateOperationPreconditionsAsync(AudioProcessingOperation operation, CancellationToken cancellationToken)
    {
        if (operation.Status is AudioProcessingOperationStatus.Succeeded)
        {
            return Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(operation, cancellationToken));
        }

        if (operation.Status is AudioProcessingOperationStatus.Processing)
        {
            return Result<AudioProcessingOperationResponse>.Conflict("The audio operation is already being processed.", "audio_operation_already_processing");
        }

        if (operation.Status is not AudioProcessingOperationStatus.Queued)
        {
            return Result<AudioProcessingOperationResponse>.Conflict("The audio operation is not ready for processing.", "audio_operation_not_queued");
        }

        return null;
    }

    // Returns the claimed operation on success, or a terminal/conflict result when the claim could not be taken.
    private async Task<(AudioProcessingOperation? Claimed, Result<AudioProcessingOperationResponse>? Failure)> ClaimOperationAsync(AudioProcessingOperation operation, CancellationToken cancellationToken)
    {
        var claimedOperation = await _audioOperationRepository.TryStartProcessingAsync(operation.Id, DateTime.UtcNow, cancellationToken);
        if (claimedOperation is not null)
        {
            return (claimedOperation, null);
        }

        var currentOperation = await _audioOperationRepository.GetByIdAsync(operation.Id, cancellationToken);
        if (currentOperation is null)
        {
            return (null, Result<AudioProcessingOperationResponse>.NotFound("Audio operation not found."));
        }

        if (currentOperation.Status is AudioProcessingOperationStatus.Succeeded or AudioProcessingOperationStatus.Failed or AudioProcessingOperationStatus.Cancelled or AudioProcessingOperationStatus.Expired)
        {
            return (null, Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(currentOperation, cancellationToken)));
        }

        return (null, Result<AudioProcessingOperationResponse>.Conflict("The audio operation is already being processed.", "audio_operation_already_processing"));
    }

    private async Task<Result<AudioProcessingOperationResponse>> ExecuteAudioOperationAsync(AudioProcessingOperation operation, CancellationToken cancellationToken)
    {
        try
        {
            await using var blobStream = await _blobStorage.OpenReadAsync(operation.BlobName, cancellationToken);
            using var memoryStream = new MemoryStream();
            await blobStream.CopyToAsync(memoryStream, cancellationToken);

            var parsedResult = await _audioDraftGenerator.ParseAsync(new KinListAudioCommand
            {
                AudioBytes = memoryStream.ToArray(),
                ContentType = operation.ContentType,
                FileName = operation.BlobName,
            }, cancellationToken);

            if (!parsedResult.IsSuccess || parsedResult.Value is null)
            {
                if (IsTerminalAudioFailure(parsedResult.Status))
                {
                    ApplyOperationFailure(operation, parsedResult.Code ?? "audio_processing_failed", parsedResult.Message ?? "Audio processing failed.");
                    operation = await _audioOperationRepository.UpdateAsync(operation, cancellationToken);
                    return Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(operation, cancellationToken));
                }

                operation = await RequeueOperationAsync(operation, cancellationToken);
                return Result<AudioProcessingOperationResponse>.ServiceUnavailable(
                    parsedResult.Message ?? "Audio processing is temporarily unavailable.",
                    parsedResult.Code ?? "audio_processing_unavailable");
            }

            var parsed = parsedResult.Value;
            var normalizedItems = NormalizeDistinctItems(parsed.Items);
            if (normalizedItems.Count is 0)
            {
                ApplyOperationFailure(operation, "no_items_detected", "No actionable list items were detected in the audio.");
                operation = await _audioOperationRepository.UpdateAsync(operation, cancellationToken);
                return Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(operation, cancellationToken));
            }

            operation.Title = parsed.Title.Trim();
            operation.ProposedItemsJson = JsonSerializer.Serialize(normalizedItems, JsonOptions);
            operation.DetectedLanguage = parsed.DetectedLanguage;
            operation.PromptVersion = parsed.PromptVersion;
            operation.ErrorCode = null;
            operation.ErrorMessage = null;
            operation.Status = AudioProcessingOperationStatus.Succeeded;
            operation.CompletedAt = DateTime.UtcNow;
            operation.UpdatedAt = DateTime.UtcNow;
            operation.Version = Guid.NewGuid();
            operation = await _audioOperationRepository.UpdateAsync(operation, cancellationToken);

            // Blob cleanup is best-effort: the operation is already Succeeded at this point.
            // A failure here must not requeue the operation (DeleteIfExistsAsync is idempotent).
            try
            {
                await _blobStorage.DeleteIfExistsAsync(operation.BlobName, cancellationToken);
            }
            catch (Exception blobEx)
            {
                _logger.LogWarning(blobEx, "Audio operation {OperationId} succeeded but blob cleanup failed; it will be retried on next pass.", operation.Id);
            }

            return Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(operation, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio operation {OperationId} failed unexpectedly and will be requeued.", operation.Id);
            operation = await RequeueOperationAsync(operation, cancellationToken);
            return Result<AudioProcessingOperationResponse>.ServiceUnavailable(ex.Message, "audio_processing_unexpected_error");
        }
    }

    public async Task<Result<AudioProcessingOperationResponse>> MarkAudioOperationFailedAsync(Guid operationId, string code, string message, CancellationToken cancellationToken = default)
    {
        var operation = await _audioOperationRepository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
        {
            return Result<AudioProcessingOperationResponse>.NotFound("Audio operation not found.");
        }

        if (operation.Status is AudioProcessingOperationStatus.Succeeded or AudioProcessingOperationStatus.Failed or AudioProcessingOperationStatus.Cancelled)
        {
            return Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(operation, cancellationToken));
        }

        ApplyOperationFailure(operation, code, message);
        operation = await _audioOperationRepository.UpdateAsync(operation, cancellationToken);
        return Result<AudioProcessingOperationResponse>.Success(await MapAudioOperationAsync(operation, cancellationToken));
    }

    public async Task<Result<KinListDraftFromAudioResponse>> CreateDraftFromAudioAsync(KinListAudioCommand command, CancellationToken cancellationToken = default)
    {
        var parsedResult = await _audioDraftGenerator.ParseAsync(command, cancellationToken);
        if (!parsedResult.IsSuccess || parsedResult.Value is null)
        {
            return MapAudioDraftFailure<KinListDraftFromAudioResponse>(parsedResult);
        }

        var parsed = parsedResult.Value;
        var normalizedItems = NormalizeDistinctItems(parsed.Items);
        if (normalizedItems.Count is 0)
        {
            return Result<KinListDraftFromAudioResponse>.UnprocessableEntity("No actionable list items were detected in the audio.", "no_items_detected");
        }

        return Result<KinListDraftFromAudioResponse>.Success(new KinListDraftFromAudioResponse
        {
            Title = parsed.Title.Trim(),
            Items = normalizedItems,
            DetectedLanguage = parsed.DetectedLanguage,
            PromptVersion = parsed.PromptVersion,
        });
    }

    public async Task<Result<KinListItemDraftsFromAudioResponse>> CreateItemDraftsFromAudioAsync(Guid listId, Guid familyId, KinListAudioCommand command, CancellationToken cancellationToken = default)
    {
        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null || list.IsDeleted)
        {
            return Result<KinListItemDraftsFromAudioResponse>.NotFound("List not found.");
        }

        if (list.FamilyId != familyId)
        {
            return Result<KinListItemDraftsFromAudioResponse>.Unauthorized("The authenticated family cannot access this list.");
        }

        var parsedResult = await _audioDraftGenerator.ParseAsync(command, cancellationToken);
        if (!parsedResult.IsSuccess || parsedResult.Value is null)
        {
            return MapAudioDraftFailure<KinListItemDraftsFromAudioResponse>(parsedResult);
        }

        var parsed = parsedResult.Value;
        var normalizedItems = NormalizeDistinctItems(parsed.Items);
        if (normalizedItems.Count is 0)
        {
            return Result<KinListItemDraftsFromAudioResponse>.UnprocessableEntity("No actionable list items were detected in the audio.", "no_items_detected");
        }

        var existingItems = await _itemRepository.GetAllByListIdAsync(listId, cancellationToken);
        var deduplication = _deduplicator.Deduplicate(normalizedItems, existingItems);

        return Result<KinListItemDraftsFromAudioResponse>.Success(new KinListItemDraftsFromAudioResponse
        {
            Items = deduplication.Proposals,
            ExistingDuplicates = deduplication.ExistingDuplicates,
            DetectedLanguage = parsed.DetectedLanguage,
            PromptVersion = parsed.PromptVersion,
        });
    }

    private static List<string> NormalizeDistinctItems(IEnumerable<string> items) =>
        items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(NormalizeText)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string NormalizeText(string text) => text.Trim();

    private async Task<AudioProcessingOperationResponse> MapAudioOperationAsync(AudioProcessingOperation operation, CancellationToken cancellationToken)
    {
        var items = DeserializeItems(operation.ProposedItemsJson);
        var response = operation.Adapt<AudioProcessingOperationResponse>();
        response.Items = items;
        response.RetryAfterSeconds = _options.AudioPollingRetryAfterSeconds;

        if (operation.Type is AudioProcessingOperationType.AppendItems && operation.ListId.HasValue && items.Count > 0)
        {
            var existingItems = await _itemRepository.GetAllByListIdAsync(operation.ListId.Value, cancellationToken);
            var deduplication = _deduplicator.Deduplicate(items, existingItems);
            response.ItemProposals = deduplication.Proposals;
            response.ExistingDuplicates = deduplication.ExistingDuplicates;
        }

        return response;
    }

    private static IReadOnlyList<string> DeserializeItems(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    private static Result<T> MapAudioDraftFailure<T>(Result<ParsedKinListAudioDraft> result) =>
        result.Status switch
        {
            ResultStatus.Conflict => Result<T>.Conflict(result.Message ?? "Audio draft request conflicted with the current state.", result.Code ?? "conflict"),
            ResultStatus.ValidationError => Result<T>.ValidationError(result.Message ?? "Audio draft request is invalid.", result.Code ?? "validation_error"),
            ResultStatus.UnprocessableEntity => Result<T>.UnprocessableEntity(result.Message ?? "Audio draft request could not be processed.", result.Code ?? "unprocessable_entity"),
            ResultStatus.Unauthorized => Result<T>.Unauthorized(result.Message ?? "The authenticated user cannot access this resource.", result.Code ?? "forbidden"),
            ResultStatus.ServiceUnavailable => Result<T>.ServiceUnavailable(result.Message ?? "Audio draft processing is unavailable.", result.Code ?? "service_unavailable"),
            ResultStatus.NotFound => Result<T>.NotFound(result.Message ?? "Audio draft dependency was not found.", result.Code ?? "not_found"),
            _ => Result<T>.UnexpectedError(result.Message ?? "Unexpected audio draft processing error.", result.Code ?? "unexpected_error"),
        };

    private static bool TryParseOperationType(string value, out AudioProcessingOperationType type)
    {
        if (string.Equals(value, "NewList", StringComparison.OrdinalIgnoreCase))
        {
            type = AudioProcessingOperationType.NewList;
            return true;
        }

        if (string.Equals(value, "AppendItems", StringComparison.OrdinalIgnoreCase))
        {
            type = AudioProcessingOperationType.AppendItems;
            return true;
        }

        type = default;
        return false;
    }

    private static string NormalizeMimeType(string contentType)
        => contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0].Trim();

    private static Result<T> ToValidationResult<T>(ValidationResult validation)
    {
        var failure = validation.Errors[0];
        return Result<T>.ValidationError(
            failure.ErrorMessage,
            string.IsNullOrWhiteSpace(failure.ErrorCode) ? "validation_error" : failure.ErrorCode);
    }

    private static void ApplyOperationFailure(AudioProcessingOperation operation, string code, string message)
    {
        operation.Status = AudioProcessingOperationStatus.Failed;
        operation.ErrorCode = code;
        operation.ErrorMessage = message;
        operation.CompletedAt = DateTime.UtcNow;
        operation.UpdatedAt = DateTime.UtcNow;
        operation.Version = Guid.NewGuid();
    }

    private static bool IsTerminalAudioFailure(ResultStatus status) =>
        status is ResultStatus.ValidationError or ResultStatus.UnprocessableEntity or ResultStatus.Unauthorized or ResultStatus.NotFound or ResultStatus.Conflict;

    private async Task<AudioProcessingOperation> RequeueOperationAsync(AudioProcessingOperation operation, CancellationToken cancellationToken)
    {
        operation.Status = AudioProcessingOperationStatus.Queued;
        operation.ErrorCode = null;
        operation.ErrorMessage = null;
        operation.CompletedAt = null;
        operation.LastHeartbeatAt = DateTime.UtcNow;
        operation.UpdatedAt = DateTime.UtcNow;
        operation.Version = Guid.NewGuid();
        return await _audioOperationRepository.UpdateAsync(operation, cancellationToken);
    }
}
