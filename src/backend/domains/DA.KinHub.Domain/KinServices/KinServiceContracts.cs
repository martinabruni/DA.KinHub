namespace DA.KinHub.Domain.KinServices;

public static class KinServiceLanguages
{
    public const string It = "it";
    public const string En = "en";
}

public sealed record KinServiceCatalogEntry(string Key, string Route, string Name, string Description);

public interface IKinServiceRepository
{
    Task<IReadOnlyList<KinServiceCatalogEntry>> GetActiveCatalogAsync(Guid familyId, string language, CancellationToken cancellationToken);

    Task<bool> IsServiceAvailableAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<KinService>> GetActivePreconfiguredAsync(CancellationToken cancellationToken);
}
