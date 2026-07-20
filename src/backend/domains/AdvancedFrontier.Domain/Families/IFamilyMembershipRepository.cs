namespace AdvancedFrontier.Domain.Families;

public interface IFamilyMembershipRepository
{
    Task<Guid?> FindActiveFamilyIdAsync(Guid applicationUserId, CancellationToken cancellationToken);

    Task<bool> HasActiveMembershipAsync(Guid applicationUserId, Guid familyId, CancellationToken cancellationToken);
}
