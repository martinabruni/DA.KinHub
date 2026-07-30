using System.Data.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.KinList;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class ActiveKinListItemRepository(KinHubDbContext dbContext) : IActiveKinListItemRepository
{
    public async Task<ActiveKinListItemsPage> GetActiveItemsPageAsync(
        Guid familyId,
        Guid applicationUserId,
        ActiveItemsCursorDirection direction,
        ActiveItemsPageAnchor? anchor,
        int effectivePageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var baseQuery = from item in dbContext.KinListItems.AsNoTracking()
                            join registrationGroup in dbContext.RegistrationGroups.AsNoTracking()
                                on new { RegistrationGroupId = item.RegistrationGroupId, item.FamilyId }
                                equals new { RegistrationGroupId = registrationGroup.Id, FamilyId = registrationGroup.FamilyId }
                            where item.FamilyId == familyId
                                && item.InactiveAt == null
                                && item.Status == ItemStatus.Active
                                && registrationGroup.InactiveAt == null
                                && (item.Visibility == ItemVisibility.Shared
                                    || (item.Visibility == ItemVisibility.Personal && item.OwnerApplicationUserId == applicationUserId))
                            select new ItemProjection(
                                item.Id,
                                item.Name.Value,
                                item.PositionInGroup,
                                item.Revision,
                                registrationGroup.CreatedAt,
                                registrationGroup.Id);

            if (anchor is not null)
            {
                baseQuery = direction == ActiveItemsCursorDirection.Next
                    ? baseQuery.Where(item =>
                        item.GroupCreatedAt < anchor.GroupCreatedAt
                        || (item.GroupCreatedAt == anchor.GroupCreatedAt && item.GroupId.CompareTo(anchor.GroupId) < 0)
                        || (item.GroupCreatedAt == anchor.GroupCreatedAt && item.GroupId == anchor.GroupId && item.PositionInGroup > anchor.PositionInGroup)
                        || (item.GroupCreatedAt == anchor.GroupCreatedAt && item.GroupId == anchor.GroupId && item.PositionInGroup == anchor.PositionInGroup && item.Id.CompareTo(anchor.ItemId) > 0))
                    : baseQuery.Where(item =>
                        item.GroupCreatedAt > anchor.GroupCreatedAt
                        || (item.GroupCreatedAt == anchor.GroupCreatedAt && item.GroupId.CompareTo(anchor.GroupId) > 0)
                        || (item.GroupCreatedAt == anchor.GroupCreatedAt && item.GroupId == anchor.GroupId && item.PositionInGroup < anchor.PositionInGroup)
                        || (item.GroupCreatedAt == anchor.GroupCreatedAt && item.GroupId == anchor.GroupId && item.PositionInGroup == anchor.PositionInGroup && item.Id.CompareTo(anchor.ItemId) < 0));
            }

            var orderedQuery = direction == ActiveItemsCursorDirection.Next
                ? baseQuery
                    .OrderByDescending(item => item.GroupCreatedAt)
                    .ThenByDescending(item => item.GroupId)
                    .ThenBy(item => item.PositionInGroup)
                    .ThenBy(item => item.Id)
                : baseQuery
                    .OrderBy(item => item.GroupCreatedAt)
                    .ThenBy(item => item.GroupId)
                    .ThenByDescending(item => item.PositionInGroup)
                    .ThenByDescending(item => item.Id);

            var projections = await orderedQuery.Take(effectivePageSize + 1).ToListAsync(cancellationToken);
            var hasMore = projections.Count > effectivePageSize;
            if (hasMore)
            {
                projections.RemoveAt(projections.Count - 1);
            }

            if (direction == ActiveItemsCursorDirection.Previous)
            {
                projections.Reverse();
            }

            var itemIds = projections.Select(item => item.Id).ToArray();
            var categories = itemIds.Length == 0
                ? []
                : await (from itemCategory in dbContext.KinListItemCategories.AsNoTracking()
                         join category in dbContext.KinListCategories.AsNoTracking()
                             on new { itemCategory.CategoryId, itemCategory.FamilyId }
                             equals new { CategoryId = category.Id, FamilyId = category.FamilyId }
                         where itemCategory.FamilyId == familyId
                             && itemIds.Contains(itemCategory.ItemId)
                             && category.InactiveAt == null
                         orderby category.CreatedAt, category.Id
                         select new CategoryProjection(itemCategory.ItemId, category.Id, category.Name))
                    .ToListAsync(cancellationToken);

            var categoriesByItem = categories
                .GroupBy(category => category.ItemId)
                .ToDictionary(
                    group => group.Key,
                    group => new CategoryPage(group.Take(3).Select(category => new ActiveKinListItemCategoryEntry(category.CategoryId, category.Name)).ToArray(), Math.Max(0, group.Count() - 3)));

            var items = projections
                .Select(item =>
                {
                    categoriesByItem.TryGetValue(item.Id, out var categoryPage);
                    return new ActiveKinListItemEntry(
                        item.Id,
                        item.Name,
                        new ActiveItemsPageAnchor(item.GroupCreatedAt, item.GroupId, item.PositionInGroup, item.Id),
                        categoryPage?.Categories ?? [],
                        categoryPage?.RemainingCategoryCount ?? 0,
                        item.Revision);
                })
                .ToArray();

            return new ActiveKinListItemsPage(items, hasMore);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The active KinList items page could not be loaded.", exception);
        }
    }

    private static bool IsRepositoryUnavailable(Exception exception)
        => exception is TimeoutException
        or DbException
        or DbUpdateException { InnerException: DbException };

    private sealed record ItemProjection(Guid Id, string Name, int PositionInGroup, long Revision, DateTimeOffset GroupCreatedAt, Guid GroupId);

    private sealed record CategoryProjection(Guid ItemId, Guid CategoryId, string Name);

    private sealed record CategoryPage(IReadOnlyList<ActiveKinListItemCategoryEntry> Categories, int RemainingCategoryCount);
}
