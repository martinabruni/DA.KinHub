using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Business.Identity;

public sealed record CreatedFamilyInvitationDto(Guid Id, string Code, DateTimeOffset ExpiresAt);

public sealed record JoinFamilyInvitationResultDto(Guid FamilyId);

public interface IFamilyInvitationService
{
    Task<CreatedFamilyInvitationDto> CreateAsync(Guid familyId, Guid applicationUserId, CancellationToken cancellationToken);

    Task RevokeAsync(Guid familyId, Guid invitationId, CancellationToken cancellationToken);

    Task<JoinFamilyInvitationResultDto> JoinAsync(ExternalIdentity externalIdentity, string? code, CancellationToken cancellationToken);
}
