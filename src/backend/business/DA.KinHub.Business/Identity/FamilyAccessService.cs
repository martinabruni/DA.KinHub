using DA.KinHub.Business.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Business.Identity;

public sealed class FamilyAccessService(
    IApplicationUserRepository applicationUserRepository,
    IFamilyMembershipRepository familyMembershipRepository) : IFamilyAccessService
{
    public async Task<FamilyAccessResult> CheckAccessAsync(ExternalIdentity externalIdentity, Guid familyId, CancellationToken cancellationToken)
    {
        try
        {
            var applicationUser = await applicationUserRepository.FindByExternalIdentityAsync(externalIdentity, cancellationToken);
            if (applicationUser is null)
            {
                return FamilyAccessResult.Denied(FamilyAccessOutcome.ProfileNotFound);
            }

            if (!applicationUser.IsActive)
            {
                return FamilyAccessResult.Denied(FamilyAccessOutcome.ProfileInactive);
            }

            return await familyMembershipRepository.HasActiveMembershipAsync(applicationUser.Id, familyId, cancellationToken)
                ? FamilyAccessResult.Granted(applicationUser.Id)
                : FamilyAccessResult.Denied(FamilyAccessOutcome.MembershipInactiveOrMissing);
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family access check failed.", exception);
        }
    }
}
