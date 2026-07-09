using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

public interface IUserCredentialRepository : IRepository<UserCredential, Guid>
{
    /// <summary>
    /// Returns the credential for the given user, or null if not found.
    /// </summary>
    Task<UserCredential?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
