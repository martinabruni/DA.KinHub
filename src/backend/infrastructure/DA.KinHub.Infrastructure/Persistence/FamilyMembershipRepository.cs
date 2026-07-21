using System.Data.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyMembershipRepository(KinHubDbContext dbContext) : IFamilyMembershipRepository
{
    public async Task<Guid?> FindActiveFamilyIdAsync(Guid applicationUserId, CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.FamilyMemberships
                .Where(membership => membership.ApplicationUserId == applicationUserId && membership.InactiveAt == null)
                .OrderBy(membership => membership.CreatedAt)
                .Select(membership => (Guid?)membership.FamilyId)
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The family membership could not be loaded.", exception);
        }
    }

    public async Task<bool> HasActiveMembershipAsync(Guid applicationUserId, Guid familyId, CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.FamilyMemberships.AnyAsync(
                membership => membership.ApplicationUserId == applicationUserId
                    && membership.FamilyId == familyId
                    && membership.InactiveAt == null,
                cancellationToken);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The family membership could not be checked.", exception);
        }
    }

    private static bool IsRepositoryUnavailable(Exception exception) =>
        exception is TimeoutException
        or DbException
        or DbUpdateException { InnerException: DbException };
}
