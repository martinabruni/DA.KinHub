using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Functions.Security;

public sealed class FamilyAuthorizationResource(Guid familyId, ExternalIdentity externalIdentity, CancellationToken cancellationToken)
{
    public Guid FamilyId { get; } = familyId;

    public ExternalIdentity ExternalIdentity { get; } = externalIdentity;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public Guid? ApplicationUserId { get; set; }
}
