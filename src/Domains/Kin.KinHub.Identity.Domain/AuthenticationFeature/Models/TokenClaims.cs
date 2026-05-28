using Kin.KinHub.Identity.Domain.Common;
namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

/// <summary>
/// Claims extracted from a validated JWT access token.
/// </summary>
public sealed record TokenClaims(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Scopes);
