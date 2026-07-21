using DA.KinHub.Business.Identity;
using Microsoft.AspNetCore.Authorization;

namespace DA.KinHub.Functions.Security;

public sealed class FamilyAuthorizationHandler(IFamilyAccessService familyAccessService) : AuthorizationHandler<FamilyAuthorizationRequirement, FamilyAuthorizationResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FamilyAuthorizationRequirement requirement,
        FamilyAuthorizationResource resource)
    {
        if (await familyAccessService.CheckAccessAsync(resource.ExternalIdentity, resource.FamilyId, resource.CancellationToken) == FamilyAccessOutcome.Granted)
        {
            context.Succeed(requirement);
        }
    }
}
