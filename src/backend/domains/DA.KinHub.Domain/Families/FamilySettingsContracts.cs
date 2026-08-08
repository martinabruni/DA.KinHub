using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.Families;

public enum FamilyPageCursorDirection
{
    Next = 0,
    Previous = 1
}

public sealed record FamilyMemberPageAnchor
{
    public FamilyMemberPageAnchor(DateTimeOffset membershipCreatedAt, Guid membershipId)
    {
        if (membershipId == Guid.Empty)
        {
            throw new DomainException("Membership ID is required.");
        }

        MembershipCreatedAt = membershipCreatedAt;
        MembershipId = membershipId;
    }

    public DateTimeOffset MembershipCreatedAt { get; }

    public Guid MembershipId { get; }
}

public sealed record FamilyInvitationPageAnchor
{
    public FamilyInvitationPageAnchor(DateTimeOffset createdAt, Guid invitationId)
    {
        if (invitationId == Guid.Empty)
        {
            throw new DomainException("Invitation ID is required.");
        }

        CreatedAt = createdAt;
        InvitationId = invitationId;
    }

    public DateTimeOffset CreatedAt { get; }

    public Guid InvitationId { get; }
}

public sealed record DecodedFamilyMemberCursor(FamilyPageCursorDirection Direction, int EffectivePageSize, FamilyMemberPageAnchor Anchor);

public sealed record DecodedFamilyInvitationCursor(FamilyPageCursorDirection Direction, int EffectivePageSize, FamilyInvitationPageAnchor Anchor);

public sealed record FamilyDetailsEntry(string Name);

public sealed record FamilyMemberEntry(Guid ApplicationUserId, string? DisplayName, string? Initials, FamilyMemberPageAnchor Anchor);

public sealed record FamilyMemberEntriesPage(IReadOnlyList<FamilyMemberEntry> Items, bool HasMore);

public sealed record FamilyInvitationCreatorEntry(string? DisplayName, string? Initials);

public sealed record FamilyInvitationEntry(
    Guid Id,
    FamilyInvitationCreatorEntry Creator,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    FamilyInvitationPageAnchor Anchor);

public sealed record FamilyInvitationEntriesPage(IReadOnlyList<FamilyInvitationEntry> Items, bool HasMore);

public interface IFamilyDetailsRepository
{
    Task<FamilyDetailsEntry?> GetFamilyDetailsAsync(Guid familyId, CancellationToken cancellationToken);
}

public interface IFamilyMemberPageRepository
{
    Task<FamilyMemberEntriesPage> GetFamilyMembersPageAsync(
        Guid familyId,
        FamilyPageCursorDirection direction,
        FamilyMemberPageAnchor? anchor,
        int effectivePageSize,
        CancellationToken cancellationToken);
}

public interface IFamilyInvitationPageRepository
{
    Task<FamilyInvitationEntriesPage> GetActiveFamilyInvitationsPageAsync(
        Guid familyId,
        FamilyPageCursorDirection direction,
        FamilyInvitationPageAnchor? anchor,
        int effectivePageSize,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
}

public interface IFamilyMemberCursorCodec
{
    string Encode(Guid familyId, FamilyPageCursorDirection direction, int effectivePageSize, FamilyMemberPageAnchor anchor);

    DecodedFamilyMemberCursor Decode(string opaqueCursor, Guid familyId);
}

public interface IFamilyInvitationCursorCodec
{
    string Encode(Guid familyId, FamilyPageCursorDirection direction, int effectivePageSize, FamilyInvitationPageAnchor anchor);

    DecodedFamilyInvitationCursor Decode(string opaqueCursor, Guid familyId);
}

public sealed class FamilyPageCursorInvalidException(string message, Exception? innerException = null) : Exception(message, innerException);
