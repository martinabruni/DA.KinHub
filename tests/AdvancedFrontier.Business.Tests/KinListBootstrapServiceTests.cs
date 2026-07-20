using AdvancedFrontier.Business.Common;
using AdvancedFrontier.Business.Identity;
using AdvancedFrontier.Domain.Families;
using AdvancedFrontier.Domain.Identity;

namespace AdvancedFrontier.Business.Tests;

public sealed class KinListBootstrapServiceTests
{
    [Fact]
    public async Task ActiveMembershipReturnsFamilyState()
    {
        var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
        var familyId = Guid.NewGuid();
        var service = new KinListBootstrapService(new StubApplicationUserRepository(user), new StubMembershipRepository(familyId), TimeProvider.System);

        var result = await service.GetBootstrapAsync(user.ExternalIdentity, CancellationToken.None);

        Assert.Equal("family", result.State);
        Assert.Equal(familyId, result.FamilyId);
    }

    [Fact]
    public async Task MissingMembershipReturnsOnboarding()
    {
        var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
        var service = new KinListBootstrapService(new StubApplicationUserRepository(user), new StubMembershipRepository(null), TimeProvider.System);

        var result = await service.GetBootstrapAsync(user.ExternalIdentity, CancellationToken.None);

        Assert.Equal("onboarding", result.State);
        Assert.Null(result.FamilyId);
    }

    [Fact]
    public async Task RepositoryFailureBecomesDependencyError()
    {
        var service = new KinListBootstrapService(new ThrowingApplicationUserRepository(), new StubMembershipRepository(null), TimeProvider.System);

        var exception = await Assert.ThrowsAsync<BusinessDependencyException>(() => service.GetBootstrapAsync(new ExternalIdentity("https://issuer", Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("dependency.postgresqlUnavailable", exception.Code);
    }

    private sealed class StubApplicationUserRepository(ApplicationUser user) : IApplicationUserRepository
    {
        public Task<ApplicationUser?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(user);

        public Task<ApplicationUser> GetOrCreateAsync(ExternalIdentity externalIdentity, DateTimeOffset createdAt, CancellationToken cancellationToken) => Task.FromResult(user);
    }

    private sealed class ThrowingApplicationUserRepository : IApplicationUserRepository
    {
        public Task<ApplicationUser?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken) => throw new InvalidOperationException("db down");

        public Task<ApplicationUser> GetOrCreateAsync(ExternalIdentity externalIdentity, DateTimeOffset createdAt, CancellationToken cancellationToken) => throw new InvalidOperationException("db down");
    }

    private sealed class StubMembershipRepository(Guid? familyId) : IFamilyMembershipRepository
    {
        public Task<Guid?> FindActiveFamilyIdAsync(Guid applicationUserId, CancellationToken cancellationToken) => Task.FromResult(familyId);

        public Task<bool> HasActiveMembershipAsync(Guid applicationUserId, Guid familyIdToCheck, CancellationToken cancellationToken) => Task.FromResult(familyId == familyIdToCheck);
    }
}
