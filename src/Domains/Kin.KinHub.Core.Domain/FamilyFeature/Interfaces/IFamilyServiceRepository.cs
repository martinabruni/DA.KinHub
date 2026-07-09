using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;
namespace Kin.KinHub.Core.Domain.FamilyFeature;

/// <summary>
/// Repository contract for <see cref="FamilyService"/> assignments.
/// </summary>
public interface IFamilyServiceRepository : IRepository<FamilyService, Guid>
{
    /// <summary>
    /// Returns all service assignments active for the given family.
    /// </summary>
    Task<IReadOnlyList<FamilyService>> GetByFamilyIdAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the service assignment for the given family and service, or null if not found.
    /// </summary>
    Task<FamilyService?> FindByFamilyAndServiceAsync(
        Guid familyId,
        int serviceId,
        CancellationToken cancellationToken = default);
}
