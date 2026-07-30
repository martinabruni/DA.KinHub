using DA.KinHub.Business.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.KinServices;

namespace DA.KinHub.Business.Identity;

public sealed class KinHubServiceCatalogService(IKinServiceRepository kinServiceRepository) : IKinHubServiceCatalogService
{
    public async Task<KinHubServiceCatalogResult> GetCatalogAsync(Guid familyId, string? language, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedLanguage = NormalizeLanguage(language);
            var services = await kinServiceRepository.GetActiveCatalogAsync(familyId, normalizedLanguage, cancellationToken);
            return new KinHubServiceCatalogResult(services.Select(service => new KinHubServiceCatalogItem(service.Key, service.Route, service.Name, service.Description)).ToArray());
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The KinService catalog could not be loaded.", exception);
        }
    }

    private static string NormalizeLanguage(string? language)
        => string.Equals(language?.Trim(), KinServiceLanguages.En, StringComparison.OrdinalIgnoreCase)
            ? KinServiceLanguages.En
            : KinServiceLanguages.It;
}
