using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

public interface IUserProviderRepository
 : IRepository<UserProvider, Guid>
{
    /// <summary>
    /// Returns every provider currently linked to the given user.
    /// </summary>
    Task<IReadOnlyList<UserProvider>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the link for the given user and provider, or null when not linked.
    /// </summary>
    Task<UserProvider?> GetByUserAndProviderAsync(Guid userId, int providerId, CancellationToken cancellationToken = default);
}
