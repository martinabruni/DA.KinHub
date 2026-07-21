using DA.KinHub.Domain.Identity;
using System.Security.Claims;

namespace DA.KinHub.Functions.Security;

public sealed class ExternalIdentityClaimsResolver
{
    public bool TryResolve(ClaimsPrincipal principal, out ExternalIdentity externalIdentity)
    {
        externalIdentity = default;

        var issuer = principal.FindFirst(SecurityConstants.IssuerClaim)?.Value?.Trim();
        var objectIdValue = principal.FindFirst(SecurityConstants.ObjectIdClaim)?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(issuer) || !Guid.TryParse(objectIdValue, out var objectId) || objectId == Guid.Empty)
        {
            return false;
        }

        externalIdentity = new ExternalIdentity(issuer, objectId);
        return true;
    }
}
