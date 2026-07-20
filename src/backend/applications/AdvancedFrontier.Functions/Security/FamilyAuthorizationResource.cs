using AdvancedFrontier.Domain.Identity;

namespace AdvancedFrontier.Functions.Security;

public sealed record FamilyAuthorizationResource(Guid FamilyId, ExternalIdentity ExternalIdentity, CancellationToken CancellationToken);
