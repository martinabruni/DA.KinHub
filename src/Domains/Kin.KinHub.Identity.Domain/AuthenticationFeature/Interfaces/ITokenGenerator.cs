using Kin.KinHub.Identity.Domain.Common;

namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

/// <summary>
/// Generates access and refresh tokens for authenticated users.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Generates a signed JWT access token containing standard claims (sub, email, roles, iss, exp) and optional scopes.
    /// </summary>
    string GenerateAccessToken(KinUser user, IReadOnlyList<string> roles, IReadOnlyList<string>? scopes = null);

    /// <summary>
    /// Generates a cryptographically-random opaque refresh token.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Returns the configured access-token lifetime in seconds.
    /// </summary>
    int AccessTokenExpirySeconds { get; }
}
