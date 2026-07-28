namespace DA.KinHub.Domain.Families;

public interface IFamilyMembershipRepository
{
    Task<Guid?> FindActiveFamilyIdAsync(Guid applicationUserId, CancellationToken cancellationToken);

    Task<bool> HasActiveMembershipAsync(Guid applicationUserId, Guid familyId, CancellationToken cancellationToken);
}

public interface IFamilyRepository
{
    Task<FamilyCreationPersistenceResult> CreateWithCreatorAsync(
        Guid applicationUserId,
        Family family,
        FamilyMembership membership,
        CancellationToken cancellationToken);
}

public abstract record FamilyCreationPersistenceResult(Guid FamilyId)
{
    public sealed record Created(Guid FamilyId) : FamilyCreationPersistenceResult(FamilyId);

    public sealed record Existing(Guid FamilyId, bool ReconciledConflict) : FamilyCreationPersistenceResult(FamilyId);
}
