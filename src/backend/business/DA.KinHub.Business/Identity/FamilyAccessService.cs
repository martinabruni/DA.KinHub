using DA.KinHub.Business.Common;
using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Business.Identity;

public sealed class FamilyAccessService(
    IApplicationUserRepository applicationUserRepository,
    IFamilyMembershipRepository familyMembershipRepository) : IFamilyAccessService
{
    public async Task<FamilyAccessOutcome> CheckAccessAsync(ExternalIdentity externalIdentity, Guid familyId, CancellationToken cancellationToken)
    {
        try
        {
            var applicationUser = await applicationUserRepository.FindByExternalIdentityAsync(externalIdentity, cancellationToken);
            if (applicationUser is null)
            {
                return FamilyAccessOutcome.ProfileNotFound;
            }

            if (!applicationUser.IsActive)
            {
                return FamilyAccessOutcome.ProfileInactive;
            }

            return await familyMembershipRepository.HasActiveMembershipAsync(applicationUser.Id, familyId, cancellationToken)
                ? FamilyAccessOutcome.Granted
                : FamilyAccessOutcome.MembershipInactiveOrMissing;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family access check failed.", exception);
        }
    }
}
