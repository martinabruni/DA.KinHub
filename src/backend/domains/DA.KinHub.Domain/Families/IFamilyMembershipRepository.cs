namespace DA.KinHub.Domain.Families;

public interface IFamilyMembershipRepository
{
    Task<Guid?> FindActiveFamilyIdAsync(Guid applicationUserId, CancellationToken cancellationToken);

    Task<bool> HasActiveMembershipAsync(Guid applicationUserId, Guid familyId, CancellationToken cancellationToken);
}

public sealed record InvitationCodeHmacCandidate(string KeyVersion, byte[] CodeHmac);

public sealed record CreatedInvitationCode(string DisplayCode, string NormalizedCode, InvitationCodeHmacCandidate Candidate);

public interface IFamilyInvitationCodeProtector
{
    CreatedInvitationCode CreateNewCode();

    string Normalize(string? code);

    IReadOnlyList<InvitationCodeHmacCandidate> CreateLookupCandidates(string normalizedCode);
}

public abstract record FamilyInvitationCreateResult
{
    public sealed record Created : FamilyInvitationCreateResult;

    public sealed record LimitReached : FamilyInvitationCreateResult;
}

public abstract record FamilyInvitationConsumeResult
{
    public sealed record Consumed(Guid FamilyId) : FamilyInvitationConsumeResult;

    public sealed record InvalidCode : FamilyInvitationConsumeResult;

    public sealed record AlreadyMember(Guid FamilyId) : FamilyInvitationConsumeResult;
}

public interface IFamilyInvitationRepository
{
    Task<FamilyInvitationCreateResult> CreateAsync(FamilyInvitation invitation, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    Task<bool> RevokeAsync(Guid familyId, Guid invitationId, DateTimeOffset revokedAt, CancellationToken cancellationToken);

    Task<FamilyInvitationConsumeResult> ConsumeAsync(Guid applicationUserId, IReadOnlyList<InvitationCodeHmacCandidate> candidates, DateTimeOffset nowUtc, CancellationToken cancellationToken);
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
