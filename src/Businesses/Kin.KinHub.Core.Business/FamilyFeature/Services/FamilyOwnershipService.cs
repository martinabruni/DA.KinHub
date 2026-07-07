using Microsoft.Extensions.Logging;

namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class FamilyOwnershipService : IFamilyOwnershipService
{
    private readonly IFamilyRepository _familyRepository;
    private readonly ILogger<FamilyOwnershipService> _logger;

    public FamilyOwnershipService(
        IFamilyRepository familyRepository,
        ILogger<FamilyOwnershipService> logger)
    {
        _familyRepository = familyRepository;
        _logger = logger;
    }

    public async Task<FamilyAccessResult> GetCurrentFamilyAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
        {
            _logger.LogWarning("Family lookup failed for user {UserId}.", userId);
            return FamilyAccessResult.NotFound("Family not found for this user.");
        }

        return FamilyAccessResult.Success(family);
    }

    public async Task<FamilyAccessResult> EnsureOwnershipAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var currentFamily = await GetCurrentFamilyAsync(userId, cancellationToken);
        return currentFamily.EnsureOwnership(
            familyId,
            ownedFamilyId => _logger.LogWarning(
                "Family ownership denied for user {UserId}. Requested family {RequestedFamilyId}, owned family {OwnedFamilyId}.",
                userId,
                familyId,
                ownedFamilyId));
    }
}
