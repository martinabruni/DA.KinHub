using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

public interface IKinUserRepository : IRepository<KinUser, Guid>
{
    /// <summary>
    /// Returns the user matching the given email, or null if not found.
    /// </summary>
    Task<KinUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}
