using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.KinServices;

namespace DA.KinHub.Business.Tests;

public sealed class KinHubServiceAccessServiceTests
{
    [Fact]
    public async Task MissingAvailabilityIsDeniedWithStableCode()
    {
        var service = new KinHubServiceAccessService(new AvailabilityRepository(false));

        var exception = await Assert.ThrowsAsync<BusinessAccessDeniedException>(() => service.EnsureAccessAsync(Guid.NewGuid(), "kinlist", CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.ServiceAccessDenied, exception.Code);
    }

    [Fact]
    public async Task RepositoryUnavailableBecomesDependencyError()
    {
        var service = new KinHubServiceAccessService(new ThrowingRepository());

        var exception = await Assert.ThrowsAsync<BusinessDependencyException>(() => service.EnsureAccessAsync(Guid.NewGuid(), "kinlist", CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.PostgreSqlUnavailable, exception.Code);
    }

    private sealed class AvailabilityRepository(bool isAvailable) : IKinServiceRepository
    {
        public Task<IReadOnlyList<KinServiceCatalogEntry>> GetActiveCatalogAsync(Guid familyId, string language, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> IsServiceAvailableAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken)
            => Task.FromResult(isAvailable);

        public Task<IReadOnlyList<KinService>> GetActivePreconfiguredAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingRepository : IKinServiceRepository
    {
        public Task<IReadOnlyList<KinServiceCatalogEntry>> GetActiveCatalogAsync(Guid familyId, string language, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> IsServiceAvailableAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken)
            => throw new RepositoryUnavailableException("db down");

        public Task<IReadOnlyList<KinService>> GetActivePreconfiguredAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
