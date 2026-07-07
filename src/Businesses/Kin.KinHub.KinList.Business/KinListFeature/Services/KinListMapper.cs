using DomainKinList = Kin.KinHub.KinList.Domain.KinListFeature.KinList;
using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IKinListMapper
{
    KinListResponse MapSummary(DomainKinList list, IReadOnlyList<DomainKinListItem> items);
    KinListDetailResponse MapDetail(DomainKinList list, IReadOnlyList<DomainKinListItem> items);
    KinListItemResponse MapItem(DomainKinListItem item);
}

public sealed class KinListMapper : IKinListMapper
{
    private readonly IEtagProvider _etagProvider;

    public KinListMapper(IEtagProvider etagProvider) => _etagProvider = etagProvider;

    public KinListResponse MapSummary(DomainKinList list, IReadOnlyList<DomainKinListItem> items)
    {
        var activeItems = items.Where(i => !i.IsDeleted).ToList();
        var completedItems = activeItems.Count(i => i.IsCompleted);
        return new KinListResponse
        {
            Id = list.Id,
            Title = list.Title,
            ETag = _etagProvider.ToEtag(list.Version),
            TotalItems = activeItems.Count,
            CompletedItems = completedItems,
            IsCompleted = activeItems.Count > 0 && completedItems == activeItems.Count,
            LastModifiedAt = list.LastModifiedAt,
        };
    }

    public KinListDetailResponse MapDetail(DomainKinList list, IReadOnlyList<DomainKinListItem> items)
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
            ETag = _etagProvider.ToEtag(list.Version),
            TotalItems = visibleItems.Count,
            CompletedItems = completedItems,
            IsCompleted = visibleItems.Count > 0 && completedItems == visibleItems.Count,
            LastModifiedAt = list.LastModifiedAt,
            Items = visibleItems,
        };
    }

    public KinListItemResponse MapItem(DomainKinListItem item) =>
        new()
        {
            Id = item.Id,
            Text = item.Text,
            ETag = _etagProvider.ToEtag(item.Version),
            IsCompleted = item.IsCompleted,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
}
