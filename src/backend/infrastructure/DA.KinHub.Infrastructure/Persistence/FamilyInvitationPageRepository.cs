using System.Data.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyInvitationPageRepository(KinHubDbContext dbContext) : IFamilyInvitationPageRepository
{
    public async Task<FamilyInvitationEntriesPage> GetActiveFamilyInvitationsPageAsync(
        Guid familyId,
        FamilyPageCursorDirection direction,
        FamilyInvitationPageAnchor? anchor,
        int effectivePageSize,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var baseQuery =
                from invitation in dbContext.FamilyInvitations.AsNoTracking()
                join family in dbContext.Families.AsNoTracking() on invitation.FamilyId equals family.Id
                where invitation.FamilyId == familyId
                    && family.InactiveAt == null
                    && invitation.RevokedAt == null
                    && invitation.ConsumedAt == null
                    && invitation.ExpiresAt > nowUtc
                select new
                {
                    invitation.Id,
                    invitation.CreatedAt,
                    invitation.ExpiresAt
                };

            if (anchor is not null)
            {
                baseQuery = direction == FamilyPageCursorDirection.Next
                    ? baseQuery.Where(item =>
                        item.CreatedAt < anchor.CreatedAt
                        || (item.CreatedAt == anchor.CreatedAt && item.Id.CompareTo(anchor.InvitationId) < 0))
                    : baseQuery.Where(item =>
                        item.CreatedAt > anchor.CreatedAt
                        || (item.CreatedAt == anchor.CreatedAt && item.Id.CompareTo(anchor.InvitationId) > 0));
            }

            var orderedQuery = direction == FamilyPageCursorDirection.Next
                ? baseQuery.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
                : baseQuery.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id);

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

            return new FamilyInvitationEntriesPage(
                rows.Select(item => new FamilyInvitationEntry(
                    item.Id,
                    new FamilyInvitationCreatorEntry(null, null),
                    item.CreatedAt,
                    item.ExpiresAt,
                    new FamilyInvitationPageAnchor(item.CreatedAt, item.Id))).ToArray(),
                hasMore);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The family invitations page could not be loaded.", exception);
        }
    }

    private static bool IsRepositoryUnavailable(Exception exception)
        => exception is TimeoutException
        or DbException
        or DbUpdateException { InnerException: DbException };
}
