using DA.KinHub.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyMembershipRepository(KinHubDbContext dbContext) : IFamilyMembershipRepository
{
    public Task<Guid?> FindActiveFamilyIdAsync(Guid applicationUserId, CancellationToken cancellationToken) =>
        dbContext.FamilyMemberships
            .Where(membership => membership.ApplicationUserId == applicationUserId && membership.InactiveAt == null)
            .OrderBy(membership => membership.CreatedAt)
            .Select(membership => (Guid?)membership.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> HasActiveMembershipAsync(Guid applicationUserId, Guid familyId, CancellationToken cancellationToken) =>
        dbContext.FamilyMemberships.AnyAsync(
            membership => membership.ApplicationUserId == applicationUserId
                && membership.FamilyId == familyId
                && membership.InactiveAt == null,
            cancellationToken);
}
