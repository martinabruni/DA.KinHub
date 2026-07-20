using AdvancedFrontier.Business.Common;
using AdvancedFrontier.Domain.Families;
using AdvancedFrontier.Domain.Identity;

namespace AdvancedFrontier.Business.Identity;

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
            throw new BusinessDependencyException("dependency.postgresqlUnavailable", "The family access check failed.", exception);
        }
    }
}
