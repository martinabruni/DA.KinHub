using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.KinServices;

namespace DA.KinHub.Business.Tests;

public sealed class KinHubServiceCatalogServiceTests
{
    [Fact]
    public async Task UnsupportedLanguageFallsBackToItalianRequest()
    {
        var repository = new StubKinServiceRepository();
        var service = new KinHubServiceCatalogService(repository);

        var result = await service.GetCatalogAsync(Guid.NewGuid(), "fr", CancellationToken.None);

        Assert.Equal("it", repository.RecordedLanguage);
        Assert.Single(result.Services);
    }

    [Fact]
    public async Task RepositoryUnavailableBecomesDependencyError()
    {
        var service = new KinHubServiceCatalogService(new ThrowingKinServiceRepository());

        var exception = await Assert.ThrowsAsync<BusinessDependencyException>(() => service.GetCatalogAsync(Guid.NewGuid(), "it", CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.PostgreSqlUnavailable, exception.Code);
    }

    private sealed class StubKinServiceRepository : IKinServiceRepository
    {
        public string? RecordedLanguage { get; private set; }

        public Task<IReadOnlyList<KinServiceCatalogEntry>> GetActiveCatalogAsync(Guid familyId, string language, CancellationToken cancellationToken)
        {
            RecordedLanguage = language;
            return Task.FromResult<IReadOnlyList<KinServiceCatalogEntry>>([new("kinlist", "/kinlist", "KinList", "Shared list")]);
        }

        public Task<bool> IsServiceAvailableAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<IReadOnlyList<KinService>> GetActivePreconfiguredAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<KinService>>([]);
    }

    private sealed class ThrowingKinServiceRepository : IKinServiceRepository
    {
        public Task<IReadOnlyList<KinServiceCatalogEntry>> GetActiveCatalogAsync(Guid familyId, string language, CancellationToken cancellationToken)
            => throw new RepositoryUnavailableException("db down");

        public Task<bool> IsServiceAvailableAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<KinService>> GetActivePreconfiguredAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
