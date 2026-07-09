using Kin.KinHub.Core.Business.Common;
using Kin.KinHub.Identity.Api.Common.Authorization;

namespace Kin.KinHub.Identity.Api.Common;

public sealed class IdentityFamilyContextResolver : IFamilyContextResolver
{
    private readonly IFamilyOwnershipService _familyOwnershipService;

    public IdentityFamilyContextResolver(IFamilyOwnershipService familyOwnershipService) =>
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
