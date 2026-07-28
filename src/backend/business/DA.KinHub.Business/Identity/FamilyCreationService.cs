using DA.KinHub.Business.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Business.Identity;

public sealed class FamilyCreationService(
    IApplicationUserRepository applicationUserRepository,
    IFamilyRepository familyRepository,
    TimeProvider timeProvider) : IFamilyCreationService
{
    public async Task<FamilyCreationResult> CreateFamilyAsync(ExternalIdentity externalIdentity, string? name, CancellationToken cancellationToken)
    {
        try
        {
            var now = timeProvider.GetUtcNow();
            var applicationUser = await applicationUserRepository.GetOrCreateAsync(externalIdentity, now, cancellationToken);
            if (!applicationUser.IsActive)
            {
                throw new BusinessAccessDeniedException("auth.profileInactive", "The signed-in profile is inactive.");
            }

            var familyName = CreateFamilyName(name);
            var family = Family.Create(familyName, applicationUser.Id, now);
            var membership = FamilyMembership.Create(applicationUser.Id, family.Id, now);
            var result = await familyRepository.CreateWithCreatorAsync(applicationUser.Id, family, membership, cancellationToken);

            return result switch
            {
                FamilyCreationPersistenceResult.Created created => FamilyCreationResult.CreatedFamily(created.FamilyId),
                FamilyCreationPersistenceResult.Existing existing => FamilyCreationResult.ExistingFamily(existing.FamilyId, existing.ReconciledConflict),
                _ => throw new InvalidOperationException("Unexpected family creation result.")
            };
        }
        catch (BusinessAccessDeniedException)
        {
            throw;
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family could not be created.", exception);
        }
    }

    private static FamilyName CreateFamilyName(string? name)
    {
        try
        {
            return FamilyName.Create(name);
        }
        catch (DomainException exception)
        {
            throw new BusinessValidationException(BusinessErrorCodes.FamilyNameInvalid, exception.Message);
        }
    }
}
