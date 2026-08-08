using System.Data.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyMemberPageRepository(KinHubDbContext dbContext) : IFamilyMemberPageRepository
{
    public async Task<FamilyMemberEntriesPage> GetFamilyMembersPageAsync(
        Guid familyId,
        FamilyPageCursorDirection direction,
        FamilyMemberPageAnchor? anchor,
        int effectivePageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var baseQuery =
                from membership in dbContext.FamilyMemberships.AsNoTracking()
                join family in dbContext.Families.AsNoTracking() on membership.FamilyId equals family.Id
                join applicationUser in dbContext.ApplicationUsers.AsNoTracking() on membership.ApplicationUserId equals applicationUser.Id
                where membership.FamilyId == familyId
                    && membership.InactiveAt == null
                    && applicationUser.InactiveAt == null
                    && family.InactiveAt == null
                select new
                {
                    membership.ApplicationUserId,
                    membership.Id,
                    membership.CreatedAt
                };

            if (anchor is not null)
            {
                baseQuery = direction == FamilyPageCursorDirection.Next
                    ? baseQuery.Where(item =>
                        item.CreatedAt > anchor.MembershipCreatedAt
                        || (item.CreatedAt == anchor.MembershipCreatedAt && item.Id.CompareTo(anchor.MembershipId) > 0))
                    : baseQuery.Where(item =>
                        item.CreatedAt < anchor.MembershipCreatedAt
                        || (item.CreatedAt == anchor.MembershipCreatedAt && item.Id.CompareTo(anchor.MembershipId) < 0));
            }

            var orderedQuery = direction == FamilyPageCursorDirection.Next
                ? baseQuery.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
                : baseQuery.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id);

            var rows = await orderedQuery.Take(effectivePageSize + 1).ToListAsync(cancellationToken);
            var hasMore = rows.Count > effectivePageSize;
            if (hasMore)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            if (direction == FamilyPageCursorDirection.Previous)
            {
                rows.Reverse();
            }

            return new FamilyMemberEntriesPage(
                rows.Select(item => new FamilyMemberEntry(item.ApplicationUserId, null, null, new FamilyMemberPageAnchor(item.CreatedAt, item.Id))).ToArray(),
                hasMore);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The family members page could not be loaded.", exception);
        }
    }

    private static bool IsRepositoryUnavailable(Exception exception)
        => exception is TimeoutException
        or DbException
        or DbUpdateException { InnerException: DbException };
}
