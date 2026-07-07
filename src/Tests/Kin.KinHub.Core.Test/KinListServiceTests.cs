using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.KinList.Domain.KinListFeature;
using DomainKinList = Kin.KinHub.KinList.Domain.KinListFeature.KinList;
using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.Core.Test;

public sealed class KinListServiceTests
{
    private static readonly KinListOptions DefaultOptions = new();

    [Fact]
    public async Task CreateAsync_WithSameIdempotencyKeyAndPayload_ReplaysStoredResponse()
    {
        var repositories = new InMemoryKinListRepositories();
        var service = CreateService(repositories);
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateKinListRequest
        {
            Title = "Spesa",
            Items = ["Latte", "Pane"],
        };

        var first = await service.CreateAsync(request, familyId, userId, "req-1");
        var second = await service.CreateAsync(request, familyId, userId, "req-1");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Equal(first.Value.ETag, second.Value.ETag);
    }

    [Fact]
    public async Task CreateAsync_WithSameIdempotencyKeyAndDifferentPayload_ReturnsConflict()
    {
        var repositories = new InMemoryKinListRepositories();
        var service = CreateService(repositories);
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await service.CreateAsync(new CreateKinListRequest { Title = "Spesa", Items = ["Latte"] }, familyId, userId, "req-1");
        var conflict = await service.CreateAsync(new CreateKinListRequest { Title = "Spesa", Items = ["Latte", "Pane"] }, familyId, userId, "req-1");

        Assert.False(conflict.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, conflict.Status);
        Assert.Equal("idempotency_conflict", conflict.Code);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenReactivated_MovesItemToTop()
    {
        var repositories = new InMemoryKinListRepositories();
        var service = CreateService(repositories);
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await service.CreateAsync(new CreateKinListRequest
        {
            Title = "Spesa",
            Items = ["Latte", "Pane"],
        }, familyId, userId, "req-1");

        var detail = created.Value!;
        var latte = detail.Items.Single(x => x.Text == "Latte");
        var pane = detail.Items.Single(x => x.Text == "Pane");

        var afterComplete = await service.UpdateItemAsync(detail.Id, latte.Id, new UpdateKinListItemRequest { Text = "Latte", IsCompleted = true }, familyId, latte.ETag);
        var completedItem = afterComplete.Value!.Items.Single(x => x.Text == "Latte");

        var afterReactivate = await service.UpdateItemAsync(detail.Id, completedItem.Id, new UpdateKinListItemRequest { Text = "Latte", IsCompleted = false }, familyId, completedItem.ETag);

        Assert.True(afterReactivate.IsSuccess);
        Assert.Equal("Latte", afterReactivate.Value!.Items.First().Text);
        Assert.Equal("Pane", afterReactivate.Value.Items.Last().Text);
        Assert.Equal(0, afterReactivate.Value.CompletedItems);
        Assert.NotEqual(pane.Id, afterReactivate.Value.Items.First().Id);
    }

    [Fact]
    public async Task BulkConfirmItemsAsync_AddsDistinctItemsToTop()
    {
        var repositories = new InMemoryKinListRepositories();
        var service = CreateService(repositories);
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await service.CreateAsync(new CreateKinListRequest
        {
            Title = "Spesa",
            Items = ["Latte"],
        }, familyId, userId, "req-1");

        var updated = await service.BulkConfirmItemsAsync(
            created.Value!.Id,
            new BulkConfirmKinListItemsRequest { Items = ["Pane", "Uova", "Pane"] },
            familyId,
            created.Value.ETag);

        Assert.True(updated.IsSuccess);
        Assert.Equal(3, updated.Value!.Items.Count);
        Assert.Equal(["Uova", "Pane", "Latte"], updated.Value.Items.Select(x => x.Text).ToArray());
    }

    [Fact]
    public async Task BulkConfirmItemsAsync_WhenListCapacityExceeded_ReturnsValidationError()
    {
        var options = new KinListOptions
        {
            MaxItemsPerList = 2,
            MaxItemsPerBulkConfirm = 2,
        };
        var repositories = new InMemoryKinListRepositories();
        var service = CreateService(repositories, options);
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await service.CreateAsync(new CreateKinListRequest
        {
            Title = "Spesa",
            Items = ["Latte"],
        }, familyId, userId, "req-1");

        var result = await service.BulkConfirmItemsAsync(
            created.Value!.Id,
            new BulkConfirmKinListItemsRequest { Items = ["Pane", "Uova"] },
            familyId,
            created.Value.ETag);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal("list_item_limit_exceeded", result.Code);
    }

    [Fact]
    public async Task CreateAsync_WithExpiredIdempotencyRecord_ReusesKeyForNewList()
    {
        var repositories = new InMemoryKinListRepositories();
        repositories.SeedExpiredRecord(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = "req-1",
            FamilyId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            RequestHash = "expired",
            ResponseJson = "{}",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        });
        var service = CreateService(repositories);
        var familyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var result = await service.CreateAsync(new CreateKinListRequest
        {
            Title = "Nuova spesa",
            Items = ["Latte"],
        }, familyId, userId, "req-1");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task CreateItemDraftsFromAudioAsync_MarksExistingDuplicatesAsDeselected()
    {
        var repositories = new InMemoryKinListRepositories();
        var audioService = CreateAudioService(
            repositories,
            new FakeKinListAudioDraftGenerator(new ParsedKinListAudioDraft
            {
                Title = "Spesa settimanale",
                Items = ["Latte", "Pane", "Uova"],
                DetectedLanguage = "it-IT",
                PromptVersion = "kinlist-audio-v1",
            }));
        var service = CreateService(repositories);
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await service.CreateAsync(new CreateKinListRequest
        {
            Title = "Spesa",
            Items = ["Latte"],
        }, familyId, userId, "req-1");

        var drafts = await audioService.CreateItemDraftsFromAudioAsync(
            created.Value!.Id,
            familyId,
            new KinListAudioCommand
            {
                AudioBytes = [1, 2, 3],
                ContentType = "audio/webm",
                FileName = "draft.webm",
            });

        Assert.True(drafts.IsSuccess);
        Assert.Equal(3, drafts.Value!.Items.Count);
        Assert.False(drafts.Value.Items.Single(x => x.Text == "Latte").IsSelectedByDefault);
        Assert.Single(drafts.Value.ExistingDuplicates);
        Assert.Equal("it-IT", drafts.Value.DetectedLanguage);
    }

    [Fact]
    public async Task CreateDraftFromAudioAsync_WhenNoItemsDetected_ReturnsUnprocessableEntity()
    {
        var repositories = new InMemoryKinListRepositories();
        var service = CreateAudioService(
            repositories,
            new FakeKinListAudioDraftGenerator(new ParsedKinListAudioDraft
            {
                Title = "Spesa",
                Items = [],
                DetectedLanguage = "it-IT",
                PromptVersion = "kinlist-audio-v1",
            }));

        var result = await service.CreateDraftFromAudioAsync(new KinListAudioCommand
        {
            AudioBytes = [1, 2, 3],
            ContentType = "audio/webm",
            FileName = "draft.webm",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.UnprocessableEntity, result.Status);
        Assert.Equal("no_items_detected", result.Code);
    }

    [Fact]
    public async Task CreateAudioOperationAsync_WhenAppendOperationHasNoListId_ReturnsValidationError()
    {
        var repositories = new InMemoryKinListRepositories();
        var service = CreateAudioService(repositories);

        var result = await service.CreateAudioOperationAsync(
            new CreateAudioProcessingOperationRequest
            {
                Type = "AppendItems",
                ContentType = "audio/webm",
                DeclaredByteSize = 3,
            },
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal("list_id_required", result.Code);
    }

    [Fact]
    public async Task ProcessAudioOperationAsync_WhenAudioGeneratorIsTransient_RequeuesOperation()
    {
        var repositories = new InMemoryKinListRepositories();
        var blobStorage = new InMemoryAudioBlobStorage();
        var queue = new InMemoryAudioProcessingQueue();
        var service = CreateAudioService(
            repositories,
            new UnavailableAudioDraftGenerator(),
            blobStorage,
            queue);
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await service.CreateAudioOperationAsync(new CreateAudioProcessingOperationRequest
        {
            Type = "NewList",
            ContentType = "audio/webm",
            DeclaredByteSize = 3,
        }, familyId, userId);
        blobStorage.Seed(created.Value!.BlobName, [1, 2, 3], "audio/webm");
        await service.CompleteAudioOperationUploadAsync(created.Value.Id, familyId);

        var processed = await service.ProcessAudioOperationAsync(created.Value.Id);
        var operation = await repositories.GetByIdAsync(created.Value.Id);

        Assert.False(processed.IsSuccess);
        Assert.Equal(ResultStatus.ServiceUnavailable, processed.Status);
        Assert.NotNull(operation);
        Assert.Equal(AudioProcessingOperationStatus.Queued, operation!.Status);
        Assert.Equal(1, operation.AttemptCount);
        Assert.Null(operation.CompletedAt);
        Assert.Null(operation.ErrorCode);
    }

    [Fact]
    public async Task ProcessAudioOperationAsync_WhenAlreadyClaimed_ReturnsConflict()
    {
        var repositories = new InMemoryKinListRepositories();
        var blobStorage = new InMemoryAudioBlobStorage();
        var queue = new InMemoryAudioProcessingQueue();
        var service = CreateAudioService(
            repositories,
            new FakeKinListAudioDraftGenerator(new ParsedKinListAudioDraft
            {
                Title = "Spesa",
                Items = ["Latte"],
                DetectedLanguage = "it-IT",
                PromptVersion = "kinlist-audio-v1",
            }),
            blobStorage,
            queue);
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await service.CreateAudioOperationAsync(new CreateAudioProcessingOperationRequest
        {
            Type = "NewList",
            ContentType = "audio/webm",
            DeclaredByteSize = 3,
        }, familyId, userId);
        blobStorage.Seed(created.Value!.BlobName, [1, 2, 3], "audio/webm");
        await service.CompleteAudioOperationUploadAsync(created.Value.Id, familyId);
        _ = await repositories.TryStartProcessingAsync(created.Value.Id, DateTime.UtcNow);

        var processed = await service.ProcessAudioOperationAsync(created.Value.Id);

        Assert.False(processed.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, processed.Status);
        Assert.Equal("audio_operation_already_processing", processed.Code);
    }

    private static KinListService CreateService(
        InMemoryKinListRepositories repositories,
        KinListOptions? options = null,
        IKinListAudioDraftGenerator? audioDraftGenerator = null)
    {
        _ = audioDraftGenerator;
        var etag = new EtagProvider();
        return new KinListService(
            repositories,
            repositories,
            repositories,
            new TestKinListTransactionExecutor(),
            new KinListMapper(etag),
            etag,
            options ?? DefaultOptions);
    }

    private static KinListAudioService CreateAudioService(
        InMemoryKinListRepositories repositories,
        IKinListAudioDraftGenerator? audioDraftGenerator = null,
        IAudioProcessingBlobStorage? blobStorage = null,
        IAudioProcessingQueue? queue = null,
        KinListOptions? options = null) =>
        new(
            repositories,
            repositories,
            repositories,
            audioDraftGenerator ?? new UnavailableAudioDraftGenerator(),
            blobStorage ?? new InMemoryAudioBlobStorage(),
            queue ?? new InMemoryAudioProcessingQueue(),
            new KinListItemDeduplicator(),
            new CorrelationIdProvider(),
            new CreateAudioProcessingOperationBusinessValidator(options ?? DefaultOptions),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<KinListAudioService>.Instance,
            options ?? DefaultOptions);
}

internal sealed class InMemoryKinListRepositories : IKinListRepository, IKinListItemRepository, IIdempotencyRecordRepository, IAudioProcessingOperationRepository
{
    private readonly Dictionary<Guid, DomainKinList> _lists = [];
    private readonly Dictionary<Guid, DomainKinListItem> _items = [];
    private readonly List<IdempotencyRecord> _records = [];
    private readonly Dictionary<Guid, AudioProcessingOperation> _audioOperations = [];

    public Task<IReadOnlyList<DomainKinList>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainKinList>>(_lists.Values.Where(x => x.FamilyId == familyId).Select(Clone).ToList());

    Task<DomainKinList?> IKinListRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_lists.TryGetValue(id, out var list) ? Clone(list) : null);

    public Task<DomainKinList> AddAsync(DomainKinList list, CancellationToken cancellationToken = default)
    {
        _lists[list.Id] = Clone(list);
        return Task.FromResult(Clone(list));
    }

    public Task<DomainKinList> UpdateAsync(DomainKinList list, CancellationToken cancellationToken = default)
    {
        _lists[list.Id] = Clone(list);
        return Task.FromResult(Clone(list));
    }

    public Task<IReadOnlyList<DomainKinListItem>> GetAllByListIdAsync(Guid listId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainKinListItem>>(_items.Values.Where(x => x.ListId == listId).OrderBy(x => x.IsCompleted).ThenByDescending(x => x.ActivationOrder).Select(Clone).ToList());

    Task<DomainKinListItem?> IKinListItemRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_items.TryGetValue(id, out var item) ? Clone(item) : null);

    public Task<DomainKinListItem> AddAsync(DomainKinListItem item, CancellationToken cancellationToken = default)
    {
        _items[item.Id] = Clone(item);
        return Task.FromResult(Clone(item));
    }

    public Task<DomainKinListItem> UpdateAsync(DomainKinListItem item, CancellationToken cancellationToken = default)
    {
        _items[item.Id] = Clone(item);
        return Task.FromResult(Clone(item));
    }

    public Task<long> GetNextActivationOrderAsync(Guid listId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Values.Where(x => x.ListId == listId && !x.IsDeleted).Select(x => (long?)x.ActivationOrder).Max() is { } max ? max + 1 : 1);

    public Task<IdempotencyRecord?> GetActiveAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.LastOrDefault(x => x.Key == key && x.FamilyId == familyId && x.UserId == userId && x.ExpiresAt > utcNow) is { } record ? Clone(record) : null);

    public Task DeleteExpiredAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        _records.RemoveAll(x => x.Key == key && x.FamilyId == familyId && x.UserId == userId && x.ExpiresAt <= utcNow);
        return Task.CompletedTask;
    }

    public Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var removed = _records.RemoveAll(x => x.ExpiresAt <= utcNow);
        return Task.FromResult(removed);
    }

    public Task<IdempotencyRecord> AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        _records.Add(Clone(record));
        return Task.FromResult(Clone(record));
    }

    public Task<AudioProcessingOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_audioOperations.TryGetValue(id, out var operation) ? Clone(operation) : null);

    public Task<AudioProcessingOperation> AddAsync(AudioProcessingOperation operation, CancellationToken cancellationToken = default)
    {
        _audioOperations[operation.Id] = Clone(operation);
        return Task.FromResult(Clone(operation));
    }

    public Task<AudioProcessingOperation> UpdateAsync(AudioProcessingOperation operation, CancellationToken cancellationToken = default)
    {
        _audioOperations[operation.Id] = Clone(operation);
        return Task.FromResult(Clone(operation));
    }

    public Task<AudioProcessingOperation?> TryStartProcessingAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (!_audioOperations.TryGetValue(id, out var operation) || operation.Status != AudioProcessingOperationStatus.Queued)
        {
            return Task.FromResult<AudioProcessingOperation?>(null);
        }

        var claimed = Clone(operation);
        claimed.Status = AudioProcessingOperationStatus.Processing;
        claimed.AttemptCount += 1;
        claimed.ProcessingStartedAt ??= utcNow;
        claimed.LastHeartbeatAt = utcNow;
        claimed.UpdatedAt = utcNow;
        claimed.Version = Guid.NewGuid();
        _audioOperations[id] = Clone(claimed);
        return Task.FromResult<AudioProcessingOperation?>(Clone(claimed));
    }

    public void SeedExpiredRecord(IdempotencyRecord record) => _records.Add(Clone(record));

    private static DomainKinList Clone(DomainKinList list) =>
        new()
        {
            Id = list.Id,
            FamilyId = list.FamilyId,
            Title = list.Title,
            Version = list.Version,
            IsDeleted = list.IsDeleted,
            CreatedAt = list.CreatedAt,
            UpdatedAt = list.UpdatedAt,
            LastModifiedAt = list.LastModifiedAt,
        };

    private static DomainKinListItem Clone(DomainKinListItem item) =>
        new()
        {
            Id = item.Id,
            ListId = item.ListId,
            Text = item.Text,
            Version = item.Version,
            IsCompleted = item.IsCompleted,
            ActivationOrder = item.ActivationOrder,
            IsDeleted = item.IsDeleted,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };

    private static IdempotencyRecord Clone(IdempotencyRecord record) =>
        new()
        {
            Id = record.Id,
            Key = record.Key,
            FamilyId = record.FamilyId,
            UserId = record.UserId,
            RequestHash = record.RequestHash,
            ResponseJson = record.ResponseJson,
            ExpiresAt = record.ExpiresAt,
            CreatedAt = record.CreatedAt,
        };

    private static AudioProcessingOperation Clone(AudioProcessingOperation operation) =>
        new()
        {
            Id = operation.Id,
            FamilyId = operation.FamilyId,
            UserId = operation.UserId,
            Type = operation.Type,
            ListId = operation.ListId,
            Status = operation.Status,
            BlobName = operation.BlobName,
            ContentType = operation.ContentType,
            DeclaredByteSize = operation.DeclaredByteSize,
            UploadedByteSize = operation.UploadedByteSize,
            Title = operation.Title,
            ProposedItemsJson = operation.ProposedItemsJson,
            DetectedLanguage = operation.DetectedLanguage,
            PromptVersion = operation.PromptVersion,
            ErrorCode = operation.ErrorCode,
            ErrorMessage = operation.ErrorMessage,
            AttemptCount = operation.AttemptCount,
            CorrelationId = operation.CorrelationId,
            Version = operation.Version,
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt,
            ExpiresAt = operation.ExpiresAt,
            UploadCompletedAt = operation.UploadCompletedAt,
            ProcessingStartedAt = operation.ProcessingStartedAt,
            CompletedAt = operation.CompletedAt,
            LastHeartbeatAt = operation.LastHeartbeatAt,
        };
}

internal sealed class TestKinListTransactionExecutor : IKinListTransactionExecutor
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        operation(cancellationToken);
}

internal sealed class FakeKinListAudioDraftGenerator : IKinListAudioDraftGenerator
{
    private readonly ParsedKinListAudioDraft _response;

    public FakeKinListAudioDraftGenerator(ParsedKinListAudioDraft response)
    {
        _response = response;
    }

    public Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<ParsedKinListAudioDraft>.Success(_response));
}

internal sealed class UnavailableAudioDraftGenerator : IKinListAudioDraftGenerator
{
    public Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<ParsedKinListAudioDraft>.ServiceUnavailable("Audio draft processing is not available.", "audio_processing_unavailable"));
}
