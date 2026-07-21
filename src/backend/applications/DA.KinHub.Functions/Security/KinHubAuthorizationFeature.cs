using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Functions.Security;

public sealed record KinHubAuthorizationFeature(ExternalIdentity ExternalIdentity, Guid? FamilyId)
{
    public Guid RequireFamilyId() => FamilyId ?? throw new InvalidOperationException("FamilyId is not available for this request.");
}
