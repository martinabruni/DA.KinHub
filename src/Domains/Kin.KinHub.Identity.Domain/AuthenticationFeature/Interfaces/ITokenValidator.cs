using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;
namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

/// <summary>
/// Validates JWT access tokens and extracts claims.
/// </summary>
public interface ITokenValidator
{
    /// <summary>
    /// Validates the given JWT and returns the extracted claims, or null if the token is invalid.
    /// </summary>
    TokenClaims? ValidateAccessToken(string token);
}
