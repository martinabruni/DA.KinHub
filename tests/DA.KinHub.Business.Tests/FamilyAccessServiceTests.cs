using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Business.Tests;

public sealed class FamilyAccessServiceTests
{
    [Fact]
    public async Task MissingUserIsReported()
    {
        var service = new FamilyAccessService(new StubApplicationUserRepository(null), new StubMembershipRepository(false));

        var result = await service.CheckAccessAsync(new ExternalIdentity("https://issuer", Guid.NewGuid()), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(FamilyAccessOutcome.ProfileNotFound, result);
    }

    [Fact]
    public async Task MembershipMismatchReturnsDenied()
    {
        var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
        var service = new FamilyAccessService(new StubApplicationUserRepository(user), new StubMembershipRepository(false));

        var result = await service.CheckAccessAsync(user.ExternalIdentity, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(FamilyAccessOutcome.MembershipInactiveOrMissing, result);
    }

    [Fact]
    public async Task RepositoryFailureBecomesDependencyError()
    {
        var service = new FamilyAccessService(new ThrowingApplicationUserRepository(), new StubMembershipRepository(false));

        var exception = await Assert.ThrowsAsync<BusinessDependencyException>(() => service.CheckAccessAsync(new ExternalIdentity("https://issuer", Guid.NewGuid()), Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("dependency.postgresqlUnavailable", exception.Code);
    }

    private sealed class StubApplicationUserRepository(ApplicationUser? user) : IApplicationUserRepository
    {
        public Task<ApplicationUser?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken) => Task.FromResult(user);

        public Task<ApplicationUser> GetOrCreateAsync(ExternalIdentity externalIdentity, DateTimeOffset createdAt, CancellationToken cancellationToken) => Task.FromResult(user ?? ApplicationUser.Create(externalIdentity, createdAt));
    }

    private sealed class ThrowingApplicationUserRepository : IApplicationUserRepository
    {
        public Task<ApplicationUser?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken) => throw new InvalidOperationException("db down");

        public Task<ApplicationUser> GetOrCreateAsync(ExternalIdentity externalIdentity, DateTimeOffset createdAt, CancellationToken cancellationToken) => throw new InvalidOperationException("db down");
    }

    private sealed class StubMembershipRepository(bool hasAccess) : IFamilyMembershipRepository
    {
        public Task<Guid?> FindActiveFamilyIdAsync(Guid applicationUserId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);

        public Task<bool> HasActiveMembershipAsync(Guid applicationUserId, Guid familyId, CancellationToken cancellationToken) => Task.FromResult(hasAccess);
    }
}
