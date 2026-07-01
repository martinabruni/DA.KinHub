using Kin.KinHub.Identity.Domain.Common;

namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

public interface IUserProviderRepository
 : IRepository<UserProvider, Guid>
{
    /// <summary>
    /// Returns every provider currently linked to the given user.
    /// </summary>
    Task<IReadOnlyList<UserProvider>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Returns the link for the given user and provider, or null when not linked.
    /// </summary>
    Task<UserProvider?> GetByUserAndProviderAsync(Guid userId, int providerId);
}
