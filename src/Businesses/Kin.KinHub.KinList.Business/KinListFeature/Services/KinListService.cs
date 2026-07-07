using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private readonly IKinListMapper _mapper;
    private readonly IEtagProvider _etagProvider;
    private readonly KinListOptions _options;

    public KinListService(
        IKinListRepository listRepository,
        IKinListItemRepository itemRepository,
        IIdempotencyRecordRepository idempotencyRepository,
        IKinListTransactionExecutor transactionExecutor,
        IKinListMapper mapper,
        IEtagProvider etagProvider,
        KinListOptions options)
    {
        _listRepository = listRepository;
        _itemRepository = itemRepository;
        _idempotencyRepository = idempotencyRepository;
        _transactionExecutor = transactionExecutor;
        _mapper = mapper;
        _etagProvider = etagProvider;
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
            var normalizedItems = KinListItemNormalizer.NormalizeDistinct(request.Items);
            if (normalizedItems.Count > _options.MaxItemsPerList)
            {
                return Result<KinListDetailResponse>.ValidationError(
                    $"A list can contain at most {_options.MaxItemsPerList} items.",
                    "list_item_limit_exceeded");
            }

            var title = request.Title.Trim();
            var requestHash = ComputeHash(title, normalizedItems);
            var now = DateTime.UtcNow;

            var replay = await TryReplayIdempotentAsync(idempotencyKey, familyId, userId, requestHash, now, ct);
            if (replay is not null)
            {
                return replay;
            }

            var (list, response) = await PersistNewListAsync(title, normalizedItems, familyId, now, ct);

            await RecordIdempotencyAsync(idempotencyKey, familyId, userId, requestHash, response, now, ct);

            return Result<KinListDetailResponse>.Success(response);
        }, cancellationToken);

    // Returns a replay result when the same Idempotency-Key was already used; null when the caller should proceed.
    private async Task<Result<KinListDetailResponse>?> TryReplayIdempotentAsync(
        string idempotencyKey, Guid familyId, Guid userId, string requestHash, DateTime now, CancellationToken ct)
    {
        await _idempotencyRepository.DeleteExpiredAsync(idempotencyKey, familyId, userId, now, ct);
        var existing = await _idempotencyRepository.GetActiveAsync(idempotencyKey, familyId, userId, now, ct);
        if (existing is null)
        {
            return null;
        }

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return Result<KinListDetailResponse>.Conflict("Idempotency-Key was already used with a different payload.", "idempotency_conflict");
        }

        var replay = JsonSerializer.Deserialize<KinListDetailResponse>(existing.ResponseJson, JsonOptions);
        return replay is null
            ? Result<KinListDetailResponse>.UnexpectedError("Stored idempotent response could not be restored.")
            : Result<KinListDetailResponse>.Success(replay);
    }

    private async Task<(DomainKinList List, KinListDetailResponse Response)> PersistNewListAsync(
        string title, IReadOnlyList<string> normalizedItems, Guid familyId, DateTime now, CancellationToken ct)
    {
        var listId = Guid.NewGuid();
        var list = new DomainKinList
        {
            Id = listId,
            FamilyId = familyId,
            Title = title,
            Version = Guid.NewGuid(),
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
            LastModifiedAt = now,
        };

        await _listRepository.AddAsync(list, ct);

        var activationOrder = normalizedItems.Count;
        foreach (var text in normalizedItems)
        {
            await _itemRepository.AddAsync(new KinListItem
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
            }, ct);
        }

        var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
        return (list, _mapper.MapDetail(list, items));
    }

    private Task RecordIdempotencyAsync(
        string idempotencyKey, Guid familyId, Guid userId, string requestHash, KinListDetailResponse response, DateTime now, CancellationToken ct)
        => _idempotencyRepository.AddAsync(new IdempotencyRecord
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
            var error = ValidateListMutation(list, familyId, ifMatch, allowDeleted: true);
            if (error is not null)
            {
                return error;
            }

            list!.IsDeleted = false;
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

            var normalizedItems = KinListItemNormalizer.NormalizeDistinct(request.Items);
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
            var listError = ValidateListAccess(list, familyId, allowDeleted: false);
            if (listError is not null)
            {
                return listError;
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

            TouchList(list!);
            await _listRepository.UpdateAsync(list!, ct);

            var items = await _itemRepository.GetAllByListIdAsync(listId, ct);
            return Result<KinListDetailResponse>.Success(_mapper.MapDetail(list!, items));
        }, cancellationToken);

    private async Task<(DomainKinList? List, DomainKinListItem? Item, Result<KinListDetailResponse>? Error)> GetItemForMutationAsync(
        Guid listId,
        Guid itemId,
        Guid familyId,
        string ifMatch,
        CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        var listError = ValidateListAccess(list, familyId);
        if (listError is not null)
        {
            return (null, null, listError);
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

    private static Result<KinListDetailResponse>? ValidateListAccess(DomainKinList? list, Guid familyId, bool allowDeleted = false)
    {
        if (list is null || (!allowDeleted && list.IsDeleted))
        {
            return Result<KinListDetailResponse>.NotFound("List not found.");
        }

        if (list.FamilyId != familyId)
        {
            return Result<KinListDetailResponse>.Unauthorized("The authenticated family cannot access this list.");
        }

        return null;
    }

    private Result<KinListDetailResponse>? ValidateListMutation(DomainKinList? list, Guid familyId, string ifMatch, bool allowDeleted = false)
    {
        var accessError = ValidateListAccess(list, familyId, allowDeleted);
        if (accessError is not null)
        {
            return accessError;
        }

        if (!MatchesEtag(list!.Version, ifMatch))
        {
            return Result<KinListDetailResponse>.Conflict("The list was modified by another request.", "etag_conflict");
        }

        return null;
    }

    private bool MatchesEtag(Guid version, string ifMatch) =>
        _etagProvider.Matches(ifMatch, version);

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

    private static string ComputeHash(string title, IReadOnlyList<string> items)
    {
        var payload = JsonSerializer.Serialize(new { title, items }, JsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
