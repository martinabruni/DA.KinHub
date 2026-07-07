namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IFamilyOwnershipService
{
    Task<FamilyAccessResult> GetCurrentFamilyAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<FamilyAccessResult> EnsureOwnershipAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
