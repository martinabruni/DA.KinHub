using DA.KinHub.Business.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.KinServices;

namespace DA.KinHub.Business.Identity;

public sealed class KinHubServiceAccessService(IKinServiceRepository kinServiceRepository) : IKinHubServiceAccessService
{
    public async Task EnsureAccessAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceKey))
        {
            throw new BusinessAccessDeniedException(BusinessErrorCodes.ServiceAccessDenied, "Access is not allowed.");
        }

        try
        {
            var allowed = await kinServiceRepository.IsServiceAvailableAsync(familyId, serviceKey, cancellationToken);
            if (!allowed)
            {
                throw new BusinessAccessDeniedException(BusinessErrorCodes.ServiceAccessDenied, "Access is not allowed.");
            }
        }
        catch (BusinessAccessDeniedException)
        {
            throw;
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The KinService access check failed.", exception);
        }
    }
}
