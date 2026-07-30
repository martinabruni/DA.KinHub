using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Functions.Security;

public sealed record KinHubAuthorizationFeature(ExternalIdentity ExternalIdentity, Guid? FamilyId, Guid? ApplicationUserId)
{
    public Guid RequireFamilyId() => FamilyId ?? throw new InvalidOperationException("FamilyId is not available for this request.");

    public Guid RequireApplicationUserId() => ApplicationUserId ?? throw new InvalidOperationException("ApplicationUserId is not available for this request.");
}
