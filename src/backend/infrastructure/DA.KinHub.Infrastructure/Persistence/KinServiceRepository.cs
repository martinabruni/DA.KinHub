using System.Data.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.KinServices;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class KinServiceRepository(KinHubDbContext dbContext) : IKinServiceRepository
{
    public async Task<IReadOnlyList<KinServiceCatalogEntry>> GetActiveCatalogAsync(Guid familyId, string language, CancellationToken cancellationToken)
    {
        try
        {
            var requestedLanguage = NormalizeLanguage(language);

            return await (from availability in dbContext.FamilyKinServiceAvailabilities
                          join service in dbContext.KinServices on availability.KinServiceId equals service.Id
                          join requested in dbContext.KinServiceLocalizations.Where(localization => localization.Language == requestedLanguage)
                              on service.Id equals requested.KinServiceId into requestedLocalizations
                          from requestedLocalization in requestedLocalizations.DefaultIfEmpty()
                          join fallback in dbContext.KinServiceLocalizations.Where(localization => localization.Language == KinServiceLanguages.En)
                              on service.Id equals fallback.KinServiceId
                          where availability.FamilyId == familyId
                              && availability.IsActive
                              && service.IsActive
                          orderby service.Route
                          select new KinServiceCatalogEntry(
                              service.Key,
                              service.Route,
                              requestedLocalization != null ? requestedLocalization.Name : fallback.Name,
                              requestedLocalization != null ? requestedLocalization.Description : fallback.Description))
                .ToListAsync(cancellationToken);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The KinService catalog could not be loaded.", exception);
        }
    }

    public async Task<bool> IsServiceAvailableAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedServiceKey = serviceKey.Trim();
            return await (from availability in dbContext.FamilyKinServiceAvailabilities
                          join service in dbContext.KinServices on availability.KinServiceId equals service.Id
                          where availability.FamilyId == familyId
                              && availability.IsActive
                              && service.IsActive
                              && service.Key == normalizedServiceKey
                          select availability.Id)
                .AnyAsync(cancellationToken);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The KinService availability could not be checked.", exception);
        }
    }

    public async Task<IReadOnlyList<KinService>> GetActivePreconfiguredAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.KinServices
                .Where(service => service.IsActive && service.IsPreconfigured)
                .OrderBy(service => service.Route)
                .ToListAsync(cancellationToken);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The preconfigured KinServices could not be loaded.", exception);
        }
    }

    private static string NormalizeLanguage(string language)
        => string.Equals(language, KinServiceLanguages.En, StringComparison.Ordinal)
            ? KinServiceLanguages.En
            : KinServiceLanguages.It;

    private static bool IsRepositoryUnavailable(Exception exception) =>
        exception is TimeoutException
        or DbException
        or DbUpdateException { InnerException: DbException };
}
