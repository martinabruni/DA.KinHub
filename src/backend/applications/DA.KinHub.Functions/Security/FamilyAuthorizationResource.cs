using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Functions.Security;

public sealed record FamilyAuthorizationResource(Guid FamilyId, ExternalIdentity ExternalIdentity, CancellationToken CancellationToken);
