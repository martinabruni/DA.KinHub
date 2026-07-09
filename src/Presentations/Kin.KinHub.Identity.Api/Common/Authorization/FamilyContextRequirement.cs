using Microsoft.AspNetCore.Authorization;

namespace Kin.KinHub.Identity.Api.Common.Authorization;

/// <summary>
/// Requires that the current request has an authenticated user with a resolved family
/// context. The <see cref="FamilyContextAuthorizationHandler"/> evaluates it and the
/// <see cref="FamilyAuthorizationMiddlewareResultHandler"/> maps failures to RFC 9457
/// problem details (401 / 403 / 503).
/// </summary>
public sealed class FamilyContextRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "FamilyContext";
}
