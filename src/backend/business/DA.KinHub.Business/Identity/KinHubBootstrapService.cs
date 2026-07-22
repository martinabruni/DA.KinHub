using DA.KinHub.Business.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Business.Identity;

public sealed class KinHubBootstrapService(
    IApplicationUserRepository applicationUserRepository,
    IFamilyMembershipRepository familyMembershipRepository,
    TimeProvider timeProvider) : IKinHubBootstrapService
{
    public async Task<KinHubBootstrapResult> GetBootstrapAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken)
    {
        try
        {
            var applicationUser = await applicationUserRepository.GetOrCreateAsync(externalIdentity, timeProvider.GetUtcNow(), cancellationToken);
            if (!applicationUser.IsActive)
            {
                throw new BusinessAccessDeniedException("auth.profileInactive", "The signed-in profile is inactive.");
            }

            var familyId = await familyMembershipRepository.FindActiveFamilyIdAsync(applicationUser.Id, cancellationToken);
            return familyId is Guid activeFamilyId
                ? KinHubBootstrapResult.Family(activeFamilyId)
                : KinHubBootstrapResult.Onboarding();
        }
        catch (BusinessAccessDeniedException)
        {
            throw;
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family context could not be loaded.", exception);
        }
    }
}
