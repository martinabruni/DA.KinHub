namespace Kin.KinHub.App.Functions.Common;

public sealed class CoreFamilyContextResolver : IFamilyContextResolver
{
    private readonly IFamilyOwnershipService _familyOwnershipService;

    public CoreFamilyContextResolver(IFamilyOwnershipService familyOwnershipService) =>
        _familyOwnershipService = familyOwnershipService;

    public async Task<FamilyContextResolution> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _familyOwnershipService.GetCurrentFamilyAsync(userId, cancellationToken);
        return result.Status switch
        {
            ResultStatus.Success when result.Family is not null => FamilyContextResolution.Success(result.Family.Id),
            ResultStatus.NotFound => FamilyContextResolution.NoFamily(),
            ResultStatus.Unauthorized => FamilyContextResolution.Forbidden(),
            _ => FamilyContextResolution.Unavailable(),
        };
    }
}
