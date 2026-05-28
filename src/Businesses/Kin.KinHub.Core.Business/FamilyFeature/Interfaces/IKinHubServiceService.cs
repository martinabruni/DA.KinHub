using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.FamilyFeature;

/// <summary>
/// Business service for KinHub service management operations.
/// </summary>
public interface IKinHubServiceService
{
    /// <summary>
    /// Returns all available KinHub services.
    /// </summary>
    Task<Result<IReadOnlyList<KinHubServiceDto>>> GetAllServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the service activation state for the given family.
    /// </summary>
    Task<Result<IReadOnlyList<FamilyServiceDto>>> GetFamilyServicesAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates a service for the given family.
    /// </summary>
    Task<Result<FamilyServiceDto>> ToggleFamilyServiceAsync(
        Guid familyId,
        ToggleFamilyServiceRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
