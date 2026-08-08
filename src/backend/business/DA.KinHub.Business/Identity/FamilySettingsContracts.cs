namespace DA.KinHub.Business.Identity;

public sealed record FamilyDetailsDto(string Name);

public sealed record FamilyMemberDto(string? DisplayName, string? Initials, bool IsCurrentUser);

public sealed record FamilyMembersPageDto(
    IReadOnlyList<FamilyMemberDto> Items,
    int EffectivePageSize,
    int MaxPageSize,
    string? PreviousCursor,
    string? NextCursor);

public sealed record FamilyInvitationCreatorDto(string? DisplayName, string? Initials);

public sealed record FamilyInvitationDto(
    Guid Id,
    FamilyInvitationCreatorDto Creator,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string Status);

public sealed record FamilyInvitationsPageDto(
    IReadOnlyList<FamilyInvitationDto> Items,
    int EffectivePageSize,
    int MaxPageSize,
    string? PreviousCursor,
    string? NextCursor);

public interface IFamilySettingsService
{
    Task<FamilyDetailsDto> GetFamilyDetailsAsync(Guid familyId, CancellationToken cancellationToken);

    Task<FamilyMembersPageDto> GetFamilyMembersPageAsync(Guid familyId, Guid applicationUserId, int requestedPageSize, string? opaqueCursor, CancellationToken cancellationToken);

    Task<FamilyInvitationsPageDto> GetActiveFamilyInvitationsPageAsync(Guid familyId, int requestedPageSize, string? opaqueCursor, CancellationToken cancellationToken);
}
