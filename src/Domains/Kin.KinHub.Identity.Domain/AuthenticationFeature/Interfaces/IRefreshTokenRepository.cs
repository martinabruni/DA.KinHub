using Kin.KinHub.Identity.Domain.Common;

namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

/// <summary>
/// Repository for persisting and querying refresh tokens.
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken, Guid>
{
    /// <summary>
    /// Returns the refresh token matching the given opaque token string, or null if not found.
    /// </summary>
    Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active refresh tokens for the given user.
    /// </summary>
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
