using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Identity.Domain.Common;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.KinList.Domain.KinListFeature;
using Kin.KinHub.App.Functions.Common.Authorization;
using DomainKinList = Kin.KinHub.KinList.Domain.KinListFeature.KinList;
using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.Core.Test;

/// <summary>Mutable <see cref="ICurrentUser"/> so a test can flip family/user context per call.</summary>
public sealed class MutableCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsAuthenticated { get; set; }
    public Guid FamilyId { get; set; }
    public bool HasFamilyContext { get; set; }
}

internal sealed class StubFamilyContextResolver : IFamilyContextResolver
{
    private readonly MutableCurrentUser _currentUser;

    public StubFamilyContextResolver(MutableCurrentUser currentUser) => _currentUser = currentUser;

    public Task<FamilyContextResolution> ResolveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            _currentUser.HasFamilyContext
                ? FamilyContextResolution.Success(_currentUser.FamilyId)
                : FamilyContextResolution.NoFamily());
}

/// <summary>Configurable audio draft generator whose parse result each test controls.</summary>
public sealed class ConfigurableAudioDraftGenerator : IKinListAudioDraftGenerator
{
    public Result<ParsedKinListAudioDraft> Result { get; set; } =
        Kin.KinHub.Shared.Kernel.Common.Result<ParsedKinListAudioDraft>.Success(new ParsedKinListAudioDraft
        {
            Title = "Spesa",
            Items = ["Latte", "Pane"],
            DetectedLanguage = "it-IT",
            PromptVersion = "kinlist-audio-v1",
        });

    public int CallCount { get; private set; }
    public TimeSpan Delay { get; set; }

    public async Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default)
    {
        CallCount++;
        if (Delay > TimeSpan.Zero)
        {
            await Task.Delay(Delay, cancellationToken);
        }

        return Result;
    }

    public void Reset()
    {
        CallCount = 0;
        Delay = TimeSpan.Zero;
    }
}

/// <summary>
/// Thread-safe in-memory implementation of all three KinList repositories. Clones on read/write
/// so callers cannot mutate stored state by reference (mirrors EF's detached behavior).
/// </summary>
public sealed class InMemoryKinListStore : IKinListRepository, IKinListItemRepository, IIdempotencyRecordRepository, IAudioProcessingOperationRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, DomainKinList> _lists = [];
    private readonly Dictionary<Guid, DomainKinListItem> _items = [];
    private readonly List<IdempotencyRecord> _records = [];
    private readonly Dictionary<Guid, AudioProcessingOperation> _audioOperations = [];

    public Task<IReadOnlyList<DomainKinList>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<DomainKinList>>(
                _lists.Values.Where(x => x.FamilyId == familyId).Select(Clone).ToList());
        }
    }

    Task<DomainKinList?> IKinListRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_lists.TryGetValue(id, out var list) ? Clone(list) : null);
        }
    }

    public Task<DomainKinList> AddAsync(DomainKinList list, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _lists[list.Id] = Clone(list);
            return Task.FromResult(Clone(list));
        }
    }

    public Task<DomainKinList> UpdateAsync(DomainKinList list, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _lists[list.Id] = Clone(list);
            return Task.FromResult(Clone(list));
        }
    }

    public Task<IReadOnlyList<DomainKinListItem>> GetAllByListIdAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<DomainKinListItem>>(
                _items.Values.Where(x => x.ListId == listId)
                    .OrderBy(x => x.IsCompleted)
                    .ThenByDescending(x => x.ActivationOrder)
                    .Select(Clone)
                    .ToList());
        }
    }

    Task<DomainKinListItem?> IKinListItemRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_items.TryGetValue(id, out var item) ? Clone(item) : null);
        }
    }

    public Task<DomainKinListItem> AddAsync(DomainKinListItem item, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _items[item.Id] = Clone(item);
            return Task.FromResult(Clone(item));
        }
    }

    public Task<DomainKinListItem> UpdateAsync(DomainKinListItem item, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _items[item.Id] = Clone(item);
            return Task.FromResult(Clone(item));
        }
    }

    public Task<long> GetNextActivationOrderAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var max = _items.Values.Where(x => x.ListId == listId && !x.IsDeleted).Select(x => (long?)x.ActivationOrder).Max();
            return Task.FromResult(max is { } value ? value + 1 : 1);
        }
    }

    public Task<IdempotencyRecord?> GetActiveAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var record = _records.LastOrDefault(x => x.Key == key && x.FamilyId == familyId && x.UserId == userId && x.ExpiresAt > utcNow);
            return Task.FromResult(record is null ? null : Clone(record));
        }
    }

    public Task DeleteExpiredAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _records.RemoveAll(x => x.Key == key && x.FamilyId == familyId && x.UserId == userId && x.ExpiresAt <= utcNow);
            return Task.CompletedTask;
        }
    }

    public Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var removed = _records.RemoveAll(x => x.ExpiresAt <= utcNow);
            return Task.FromResult(removed);
        }
    }

    public Task<IdempotencyRecord> AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _records.Add(Clone(record));
            return Task.FromResult(Clone(record));
        }
    }

    public int IdempotencyRecordCount
    {
        get
        {
            lock (_gate)
            {
                return _records.Count;
            }
        }
    }

    public void SeedIdempotencyRecord(IdempotencyRecord record)
    {
        lock (_gate)
        {
            _records.Add(Clone(record));
        }
    }

    public Task<AudioProcessingOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_audioOperations.TryGetValue(id, out var operation) ? Clone(operation) : null);
        }
    }

    public Task<AudioProcessingOperation> AddAsync(AudioProcessingOperation operation, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _audioOperations[operation.Id] = Clone(operation);
            return Task.FromResult(Clone(operation));
        }
    }

    public Task<AudioProcessingOperation> UpdateAsync(AudioProcessingOperation operation, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _audioOperations[operation.Id] = Clone(operation);
            return Task.FromResult(Clone(operation));
        }
    }

    public Task<AudioProcessingOperation?> TryStartProcessingAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        lock (_gate)
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
    }

    private static DomainKinList Clone(DomainKinList list) => new()
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

    private static DomainKinListItem Clone(DomainKinListItem item) => new()
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

    private static IdempotencyRecord Clone(IdempotencyRecord record) => new()
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

    private static AudioProcessingOperation Clone(AudioProcessingOperation operation) => new()
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

public sealed class InMemoryAudioBlobStorage : IAudioProcessingBlobStorage
{
    private readonly Dictionary<string, (byte[] Data, string ContentType)> _blobs = [];

    public Task<AudioBlobUploadTarget> CreateUploadTargetAsync(string blobName, string contentType, TimeSpan timeToLive, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AudioBlobUploadTarget
        {
            BlobName = blobName,
            UploadUrl = new Uri($"https://example.test/{Uri.EscapeDataString(blobName)}"),
            ExpiresAt = DateTime.UtcNow.Add(timeToLive),
        });

    public Task<AudioBlobDescriptor?> GetBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        if (!_blobs.TryGetValue(blobName, out var blob))
        {
            return Task.FromResult<AudioBlobDescriptor?>(null);
        }

        return Task.FromResult<AudioBlobDescriptor?>(new AudioBlobDescriptor
        {
            BlobName = blobName,
            ContentType = blob.ContentType,
            ContentLength = blob.Data.LongLength,
        });
    }

    public Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blob = _blobs[blobName];
        return Task.FromResult<Stream>(new MemoryStream(blob.Data, writable: false));
    }

    public Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        _blobs.Remove(blobName);
        return Task.CompletedTask;
    }

    public void Seed(string blobName, byte[] data, string contentType)
    {
        _blobs[blobName] = (data, contentType);
    }

    public void Reset()
    {
        _blobs.Clear();
    }
}

public sealed class InMemoryAudioProcessingQueue : IAudioProcessingQueue
{
    public List<(Guid OperationId, string CorrelationId)> Messages { get; } = [];

    public Task EnqueueAsync(Guid operationId, string correlationId, CancellationToken cancellationToken = default)
    {
        Messages.Add((operationId, correlationId));
        return Task.CompletedTask;
    }

    public void Reset()
    {
        Messages.Clear();
    }
}
