using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Domain.KinListFeature;
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
    private readonly IKinListAudioDraftGenerator _audioDraftGenerator;
    private readonly KinListOptions _options;

    public KinListService(
        IKinListRepository listRepository,
        IKinListItemRepository itemRepository,
        IIdempotencyRecordRepository idempotencyRepository,
        IKinListTransactionExecutor transactionExecutor,
        IKinListAudioDraftGenerator audioDraftGenerator,
        KinListOptions options)
    {
        _listRepository = listRepository;
        _itemRepository = itemRepository;
        _idempotencyRepository = idempotencyRepository;
        _transactionExecutor = transactionExecutor;
        _audioDraftGenerator = audioDraftGenerator;
        _options = options;
    }

    public async Task<Result<IReadOnlyList<KinListResponse>>> GetAllAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var lists = await _listRepository.GetAllByFamilyIdAsync(familyId, cancellationToken);
        var responses = new List<KinListResponse>(lists.Count);

        foreach (var list in lists.Where(l => !l.IsDeleted))
        {
            var items = await _itemRepository.GetAllByListIdAsync(list.Id, cancellationToken);
            responses.Add(MapSummary(list, items));
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
        return Result<KinListDetailResponse>.Success(MapDetail(list, items));
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
            var response = MapDetail(list, items);

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
            return Result<KinListDetailResponse>.Success(MapDetail(list, items));
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
            return Result<KinListDetailResponse>.Success(MapDetail(list, items));
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
            return Result<KinListDetailResponse>.Success(MapDetail(list, items));
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
            return Result<KinListDetailResponse>.Success(MapDetail(list!, items));
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
            return Result<KinListDetailResponse>.Success(MapDetail(list!, items));
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
            return Result<KinListDetailResponse>.Success(MapDetail(list!, items));
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
            return Result<KinListDetailResponse>.Success(MapDetail(list!, items));
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
            return Result<KinListDetailResponse>.Success(MapDetail(list, items));
        }, cancellationToken);

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

        var existingItems = (await _itemRepository.GetAllByListIdAsync(listId, cancellationToken))
            .Where(x => !x.IsDeleted)
            .ToList();
        var normalizedExistingItems = existingItems
            .GroupBy(x => NormalizeText(x.Text), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var proposals = new List<KinListItemDraftProposalResponse>(normalizedItems.Count);
        var duplicates = new List<KinListExistingDuplicateResponse>();
        foreach (var text in normalizedItems)
        {
            var normalizedText = NormalizeText(text);
            if (normalizedExistingItems.TryGetValue(normalizedText, out var duplicateItem))
            {
                proposals.Add(new KinListItemDraftProposalResponse
                {
                    Text = text,
                    IsSelectedByDefault = false,
                    DuplicateOfItemId = duplicateItem.Id,
                });

                duplicates.Add(new KinListExistingDuplicateResponse
                {
                    ItemId = duplicateItem.Id,
                    Text = duplicateItem.Text,
                    IsCompleted = duplicateItem.IsCompleted,
                });

                continue;
            }

            proposals.Add(new KinListItemDraftProposalResponse
            {
                Text = text,
                IsSelectedByDefault = true,
            });
        }

        return Result<KinListItemDraftsFromAudioResponse>.Success(new KinListItemDraftsFromAudioResponse
        {
            Items = proposals,
            ExistingDuplicates = duplicates,
            DetectedLanguage = parsed.DetectedLanguage,
            PromptVersion = parsed.PromptVersion,
        });
    }

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

    private static Result<KinListDetailResponse>? ValidateListMutation(DomainKinList? list, Guid familyId, string ifMatch)
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

    private static bool MatchesEtag(Guid version, string ifMatch) =>
        string.Equals(ToEtag(version), ifMatch.Trim(), StringComparison.Ordinal);

    private static void TouchList(DomainKinList list)
    {
        var now = DateTime.UtcNow;
        list.Version = Guid.NewGuid();
        list.UpdatedAt = now;
        list.LastModifiedAt = now;
    }

    private static KinListResponse MapSummary(DomainKinList list, IReadOnlyList<DomainKinListItem> items)
    {
        var activeItems = items.Where(i => !i.IsDeleted).ToList();
        var completedItems = activeItems.Count(i => i.IsCompleted);
        return new KinListResponse
        {
            Id = list.Id,
            Title = list.Title,
            ETag = ToEtag(list.Version),
            TotalItems = activeItems.Count,
            CompletedItems = completedItems,
            IsCompleted = activeItems.Count > 0 && completedItems == activeItems.Count,
            LastModifiedAt = list.LastModifiedAt,
        };
    }

    private static KinListDetailResponse MapDetail(DomainKinList list, IReadOnlyList<DomainKinListItem> items)
    {
        var visibleItems = items
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.IsCompleted)
            .ThenByDescending(i => i.ActivationOrder)
            .ThenBy(i => i.CreatedAt)
            .Select(MapItem)
            .ToList();

        var completedItems = visibleItems.Count(i => i.IsCompleted);
        return new KinListDetailResponse
        {
            Id = list.Id,
            Title = list.Title,
            ETag = ToEtag(list.Version),
            TotalItems = visibleItems.Count,
            CompletedItems = completedItems,
            IsCompleted = visibleItems.Count > 0 && completedItems == visibleItems.Count,
            LastModifiedAt = list.LastModifiedAt,
            Items = visibleItems,
        };
    }

    private static KinListItemResponse MapItem(DomainKinListItem item) =>
        new()
        {
            Id = item.Id,
            Text = item.Text,
            ETag = ToEtag(item.Version),
            IsCompleted = item.IsCompleted,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };

    private static string ToEtag(Guid version) => $"\"{version:D}\"";

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

    private static string ComputeHash(string title, IReadOnlyList<string> items)
    {
        var payload = JsonSerializer.Serialize(new { title, items }, JsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
