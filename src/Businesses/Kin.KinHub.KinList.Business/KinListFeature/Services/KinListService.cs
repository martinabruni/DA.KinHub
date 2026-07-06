using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kin.KinHub.KinList.Business.Common;
using DomainKinList = Kin.KinHub.KinList.Domain.KinListFeature.KinList;
using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListService : IKinListService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IKinListRepository _listRepository;
    private readonly IKinListItemRepository _itemRepository;
    private readonly IIdempotencyRecordRepository _idempotencyRepository;
    private readonly IKinListTransactionExecutor _transactionExecutor;
    private readonly IKinListAudioService _audioService;
    private readonly IKinListMapper _mapper;
    private readonly KinListOptions _options;

    public KinListService(
        IKinListRepository listRepository,
        IKinListItemRepository itemRepository,
        IIdempotencyRecordRepository idempotencyRepository,
        IAudioProcessingOperationRepository audioOperationRepository,
        IKinListTransactionExecutor transactionExecutor,
        IKinListAudioDraftGenerator audioDraftGenerator,
        IAudioProcessingBlobStorage blobStorage,
        IAudioProcessingQueue audioQueue,
        KinListOptions options,
        IKinListMapper? mapper = null,
        IKinListItemDeduplicator? deduplicator = null,
        IKinListAudioService? audioService = null)
    {
        _listRepository = listRepository;
        _itemRepository = itemRepository;
        _idempotencyRepository = idempotencyRepository;
        _transactionExecutor = transactionExecutor;
        _mapper = mapper ?? new KinListMapper();
        _audioService = audioService ?? new KinListAudioService(
            listRepository,
            itemRepository,
            audioOperationRepository,
            audioDraftGenerator,
            blobStorage,
            audioQueue,
            deduplicator ?? new KinListItemDeduplicator(),
            options);
        _options = options;
    }

    public async Task<Result<IReadOnlyList<KinListResponse>>> GetAllAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var lists = await _listRepository.GetAllByFamilyIdAsync(familyId, cancellationToken);
        var responses = new List<KinListResponse>(lists.Count);

        foreach (var list in lists.Where(l => !l.IsDeleted))
        {
            var items = await _itemRepository.GetAllByListIdAsync(list.Id, cancellationToken);
            responses.Add(_mapper.MapSummary(list, items));
        }

        var ordered = responses
            .OrderBy(r => r.IsCompleted)
            .ThenByDescending(r => r.LastModifiedAt)
            .ToList();

        return Result<IReadOnlyList<KinListResponse>>.Success(ordered);
    }

    public async Task<Result<KinListDetailResponse>> GetByIdAsync(Guid listId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null || list.IsDeleted)
        {
            return Result<KinListDetailResponse>.NotFound("List not found.");
        }

        if (list.FamilyId != familyId)
        {
            return Result<KinListDetailResponse>.Unauthorized("The authenticated family cannot access this list.");
        }

        var items = await _itemRepository.GetAllByListIdAsync(listId, cancellationToken);
        return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list, items));
    }

    public async Task<Result<KinListDetailResponse>> CreateAsync(CreateKinListRequest request, Guid familyId, Guid userId, string idempotencyKey, CancellationToken cancellationToken = default)
        => await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var normalizedItems = NormalizeDistinctItems(request.Items);
            if (normalizedItems.Count > _options.MaxItemsPerList)
            {
                return Result<KinListDetailResponse>.ValidationError(
                    $"A list can contain at most {_options.MaxItemsPerList} items.",
                    "list_item_limit_exceeded");
            }

            var requestHash = ComputeHash(request.Title.Trim(), normalizedItems);
            var now = DateTime.UtcNow;
            await _idempotencyRepository.DeleteExpiredAsync(idempotencyKey, familyId, userId, now, ct);
            var existing = await _idempotencyRepository.GetActiveAsync(idempotencyKey, familyId, userId, now, ct);
            if (existing is not null)
            {
                if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                {
                    return Result<KinListDetailResponse>.Conflict("Idempotency-Key was already used with a different payload.", "idempotency_conflict");
                }

                var replay = JsonSerializer.Deserialize<KinListDetailResponse>(existing.ResponseJson, JsonOptions);
                if (replay is null)
                {
                    return Result<KinListDetailResponse>.UnexpectedError("Stored idempotent response could not be restored.");
                }

                return Result<KinListDetailResponse>.Success(replay);
            }

            var listId = Guid.NewGuid();
            var version = Guid.NewGuid();
            var list = new DomainKinList
            {
                Id = listId,
                FamilyId = familyId,
                Title = request.Title.Trim(),
                Version = version,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now,
                LastModifiedAt = now,
            };

            await _listRepository.AddAsync(list, ct);

            var activationOrder = normalizedItems.Count;
            foreach (var text in normalizedItems)
            {
                var item = new KinListItem
                {
                    Id = Guid.NewGuid(),
                    ListId = listId,
                    Text = text,
                    Version = Guid.NewGuid(),
                    IsCompleted = false,
                    ActivationOrder = activationOrder--,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                await _itemRepository.AddAsync(item, ct);
            }

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            var response = _mapper.MapDetail(list, items);

            await _idempotencyRepository.AddAsync(new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                Key = idempotencyKey,
                FamilyId = familyId,
                UserId = userId,
                RequestHash = requestHash,
                ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
                ExpiresAt = now.AddHours(_options.IdempotencyRetentionHours),
                CreatedAt = now,
            }, ct);

            return Result<KinListDetailResponse>.Success(response);
        }, cancellationToken);

    public async Task<Result<KinListDetailResponse>> UpdateAsync(Guid listId, UpdateKinListRequest request, Guid familyId, string ifMatch, CancellationToken cancellationToken = default)
        => await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var list = await _listRepository.GetByIdAsync(listId, ct);
            var error = ValidateListMutation(list, familyId, ifMatch);
            if (error is not null)
            {
                return error;
            }

            list!.Title = request.Title.Trim();
            TouchList(list);
            await _listRepository.UpdateAsync(list, ct);

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list, items));
        }, cancellationToken);

    public async Task<Result<KinListDetailResponse>> DeleteAsync(Guid listId, Guid familyId, string ifMatch, CancellationToken cancellationToken = default)
        => await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var list = await _listRepository.GetByIdAsync(listId, ct);
            var error = ValidateListMutation(list, familyId, ifMatch);
            if (error is not null)
            {
                return error;
            }

            list!.IsDeleted = true;
            TouchList(list);
            await _listRepository.UpdateAsync(list, ct);

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list, items));
        }, cancellationToken);

    public async Task<Result<KinListDetailResponse>> RestoreAsync(Guid listId, Guid familyId, string ifMatch, CancellationToken cancellationToken = default)
        => await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var list = await _listRepository.GetByIdAsync(listId, ct);
            if (list is null)
            {
                return Result<KinListDetailResponse>.NotFound("List not found.");
            }

            if (list.FamilyId != familyId)
            {
                return Result<KinListDetailResponse>.Unauthorized("The authenticated family cannot access this list.");
            }

            if (!MatchesEtag(list.Version, ifMatch))
            {
                return Result<KinListDetailResponse>.Conflict("The list was modified by another request.", "etag_conflict");
            }

            list.IsDeleted = false;
            TouchList(list);
            await _listRepository.UpdateAsync(list, ct);

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list, items));
        }, cancellationToken);

    public async Task<Result<KinListDetailResponse>> AddItemAsync(Guid listId, CreateKinListItemRequest request, Guid familyId, string ifMatch, CancellationToken cancellationToken = default)
        => await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var list = await _listRepository.GetByIdAsync(listId, ct);
            var error = ValidateListMutation(list, familyId, ifMatch);
            if (error is not null)
            {
                return error;
            }

            var visibleItemCount = await CountVisibleItemsAsync(listId, ct);
            if (visibleItemCount >= _options.MaxItemsPerList)
            {
                return Result<KinListDetailResponse>.ValidationError(
                    $"A list can contain at most {_options.MaxItemsPerList} items.",
                    "list_item_limit_exceeded");
            }

            var now = DateTime.UtcNow;
            var item = new KinListItem
            {
                Id = Guid.NewGuid(),
                ListId = listId,
                Text = request.Text.Trim(),
                Version = Guid.NewGuid(),
                IsCompleted = false,
                ActivationOrder = await _itemRepository.GetNextActivationOrderAsync(listId, ct),
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _itemRepository.AddAsync(item, ct);
            TouchList(list!);
            await _listRepository.UpdateAsync(list!, ct);

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list!, items));
        }, cancellationToken);

    public async Task<Result<KinListDetailResponse>> BulkConfirmItemsAsync(Guid listId, BulkConfirmKinListItemsRequest request, Guid familyId, string ifMatch, CancellationToken cancellationToken = default)
        => await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var list = await _listRepository.GetByIdAsync(listId, ct);
            var error = ValidateListMutation(list, familyId, ifMatch);
            if (error is not null)
            {
                return error;
            }

            var normalizedItems = NormalizeDistinctItems(request.Items);
            if (normalizedItems.Count > _options.MaxItemsPerBulkConfirm)
            {
                return Result<KinListDetailResponse>.ValidationError(
                    $"A bulk confirm operation can contain at most {_options.MaxItemsPerBulkConfirm} items.",
                    "bulk_confirm_limit_exceeded");
            }

            var visibleItemCount = await CountVisibleItemsAsync(listId, ct);
            if (visibleItemCount + normalizedItems.Count > _options.MaxItemsPerList)
            {
                return Result<KinListDetailResponse>.ValidationError(
                    $"A list can contain at most {_options.MaxItemsPerList} items.",
                    "list_item_limit_exceeded");
            }

            var nextActivationOrder = await _itemRepository.GetNextActivationOrderAsync(listId, ct);
            var now = DateTime.UtcNow;
            foreach (var text in normalizedItems)
            {
                await _itemRepository.AddAsync(new KinListItem
                {
                    Id = Guid.NewGuid(),
                    ListId = listId,
                    Text = text,
                    Version = Guid.NewGuid(),
                    IsCompleted = false,
                    ActivationOrder = nextActivationOrder++,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                }, ct);
            }

            TouchList(list!);
            await _listRepository.UpdateAsync(list!, ct);

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list!, items));
        }, cancellationToken);

    public async Task<Result<KinListDetailResponse>> UpdateItemAsync(Guid listId, Guid itemId, UpdateKinListItemRequest request, Guid familyId, string ifMatch, CancellationToken cancellationToken = default)
        => await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var (list, item, error) = await GetItemForMutationAsync(listId, itemId, familyId, ifMatch, ct);
            if (error is not null)
            {
                return error;
            }

            item!.Text = request.Text.Trim();

            if (!item.IsCompleted && request.IsCompleted)
            {
                item.IsCompleted = true;
            }
            else if (item.IsCompleted && !request.IsCompleted)
            {
                item.IsCompleted = false;
                item.ActivationOrder = await _itemRepository.GetNextActivationOrderAsync(listId, ct);
            }

            item.Version = Guid.NewGuid();
            item.UpdatedAt = DateTime.UtcNow;
            await _itemRepository.UpdateAsync(item, ct);

            TouchList(list!);
            await _listRepository.UpdateAsync(list!, ct);

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list!, items));
        }, cancellationToken);

    public async Task<Result<KinListDetailResponse>> DeleteItemAsync(Guid listId, Guid itemId, Guid familyId, string ifMatch, CancellationToken cancellationToken = default)
        => await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var (list, item, error) = await GetItemForMutationAsync(listId, itemId, familyId, ifMatch, ct);
            if (error is not null)
            {
                return error;
            }

            item!.IsDeleted = true;
            item.Version = Guid.NewGuid();
            item.UpdatedAt = DateTime.UtcNow;
            await _itemRepository.UpdateAsync(item, ct);

            TouchList(list!);
            await _listRepository.UpdateAsync(list!, ct);

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list!, items));
        }, cancellationToken);

    public async Task<Result<KinListDetailResponse>> RestoreItemAsync(Guid listId, Guid itemId, Guid familyId, string ifMatch, CancellationToken cancellationToken = default)
        => await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var list = await _listRepository.GetByIdAsync(listId, ct);
            if (list is null || list.IsDeleted)
            {
                return Result<KinListDetailResponse>.NotFound("List not found.");
            }

            if (list.FamilyId != familyId)
            {
                return Result<KinListDetailResponse>.Unauthorized("The authenticated family cannot access this list.");
            }

            var item = await _itemRepository.GetByIdAsync(itemId, ct);
            if (item is null || item.ListId != listId)
            {
                return Result<KinListDetailResponse>.NotFound("Item not found.");
            }

            if (!MatchesEtag(item.Version, ifMatch))
            {
                return Result<KinListDetailResponse>.Conflict("The item was modified by another request.", "etag_conflict");
            }

            item.IsDeleted = false;
            item.IsCompleted = false;
            item.ActivationOrder = await _itemRepository.GetNextActivationOrderAsync(listId, ct);
            item.Version = Guid.NewGuid();
            item.UpdatedAt = DateTime.UtcNow;
            await _itemRepository.UpdateAsync(item, ct);

            TouchList(list);
            await _listRepository.UpdateAsync(list, ct);

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list, items));
        }, cancellationToken);

    public Task<Result<CreateAudioProcessingOperationResponse>> CreateAudioOperationAsync(CreateAudioProcessingOperationRequest request, Guid familyId, Guid userId, CancellationToken cancellationToken = default) =>
        _audioService.CreateAudioOperationAsync(request, familyId, userId, cancellationToken);

    public Task<Result<AudioProcessingOperationResponse>> CompleteAudioOperationUploadAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default) =>
        _audioService.CompleteAudioOperationUploadAsync(operationId, familyId, cancellationToken);

    public Task<Result<AudioProcessingOperationResponse>> GetAudioOperationAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default) =>
        _audioService.GetAudioOperationAsync(operationId, familyId, cancellationToken);

    public Task<Result<bool>> DeleteAudioOperationAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default) =>
        _audioService.DeleteAudioOperationAsync(operationId, familyId, cancellationToken);

    public Task<Result<AudioProcessingOperationResponse>> ProcessAudioOperationAsync(Guid operationId, CancellationToken cancellationToken = default) =>
        _audioService.ProcessAudioOperationAsync(operationId, cancellationToken);

    public Task<Result<AudioProcessingOperationResponse>> MarkAudioOperationFailedAsync(Guid operationId, string code, string message, CancellationToken cancellationToken = default) =>
        _audioService.MarkAudioOperationFailedAsync(operationId, code, message, cancellationToken);

    public Task<Result<KinListDraftFromAudioResponse>> CreateDraftFromAudioAsync(KinListAudioCommand command, CancellationToken cancellationToken = default) =>
        _audioService.CreateDraftFromAudioAsync(command, cancellationToken);

    public Task<Result<KinListItemDraftsFromAudioResponse>> CreateItemDraftsFromAudioAsync(Guid listId, Guid familyId, KinListAudioCommand command, CancellationToken cancellationToken = default) =>
        _audioService.CreateItemDraftsFromAudioAsync(listId, familyId, command, cancellationToken);

    private async Task<(DomainKinList? List, DomainKinListItem? Item, Result<KinListDetailResponse>? Error)> GetItemForMutationAsync(
        Guid listId,
        Guid itemId,
        Guid familyId,
        string ifMatch,
        CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null || list.IsDeleted)
        {
            return (null, null, Result<KinListDetailResponse>.NotFound("List not found."));
        }

        if (list.FamilyId != familyId)
        {
            return (null, null, Result<KinListDetailResponse>.Unauthorized("The authenticated family cannot access this list."));
        }

        var item = await _itemRepository.GetByIdAsync(itemId, cancellationToken);
        if (item is null || item.ListId != listId || item.IsDeleted)
        {
            return (null, null, Result<KinListDetailResponse>.NotFound("Item not found."));
        }

        if (!MatchesEtag(item.Version, ifMatch))
        {
            return (null, null, Result<KinListDetailResponse>.Conflict("The item was modified by another request.", "etag_conflict"));
        }

        return (list, item, null);
    }

    private Result<KinListDetailResponse>? ValidateListMutation(DomainKinList? list, Guid familyId, string ifMatch)
    {
        if (list is null || list.IsDeleted)
        {
            return Result<KinListDetailResponse>.NotFound("List not found.");
        }

        if (list.FamilyId != familyId)
        {
            return Result<KinListDetailResponse>.Unauthorized("The authenticated family cannot access this list.");
        }

        if (!MatchesEtag(list.Version, ifMatch))
        {
            return Result<KinListDetailResponse>.Conflict("The list was modified by another request.", "etag_conflict");
        }

        return null;
    }

    private bool MatchesEtag(Guid version, string ifMatch) =>
        string.Equals(_mapper.ToEtag(version), ifMatch.Trim(), StringComparison.Ordinal);

    private static void TouchList(DomainKinList list)
    {
        var now = DateTime.UtcNow;
        list.Version = Guid.NewGuid();
        list.UpdatedAt = now;
        list.LastModifiedAt = now;
    }

    private async Task<int> CountVisibleItemsAsync(Guid listId, CancellationToken cancellationToken)
    {
        var items = await _itemRepository.GetAllByListIdAsync(listId, cancellationToken);
        return items.Count(i => !i.IsDeleted);
    }

    private static List<string> NormalizeDistinctItems(IEnumerable<string> items) =>
        items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(NormalizeText)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string NormalizeText(string text) => text.Trim();

    private static string ComputeHash(string title, IReadOnlyList<string> items)
    {
        var payload = JsonSerializer.Serialize(new { title, items }, JsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
