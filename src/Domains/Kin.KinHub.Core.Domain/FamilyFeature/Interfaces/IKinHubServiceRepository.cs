using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;
namespace Kin.KinHub.Core.Domain.FamilyFeature;

/// <summary>
/// Repository contract for <see cref="KinHubService"/> configuration entries.
/// </summary>
public interface IKinHubServiceRepository : IRepository<KinHubService, int>
{
    /// <summary>
    /// Returns the service matching the given service type, or null if not found.
    /// </summary>
    Task<KinHubService?> FindByServiceTypeAsync(
        KinHubServiceType serviceType,
        CancellationToken cancellationToken = default);
}
