using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Business.Tests;

public sealed class FamilyCreationServiceTests
{
    [Fact]
    public async Task ValidCreateReturnsCreated()
    {
        var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
        var repository = new StubFamilyRepository(new FamilyCreationPersistenceResult.Created(Guid.NewGuid()));
        var service = new FamilyCreationService(new StubApplicationUserRepository(user), repository, TimeProvider.System);

        var result = await service.CreateFamilyAsync(user.ExternalIdentity, "Famiglia Bruni", CancellationToken.None);

        Assert.True(result.Created);
        Assert.False(result.ReconciledConflict);
        Assert.Equal("Famiglia Bruni", repository.RecordedFamily!.Name.Value);
    }

    [Fact]
    public async Task ExistingMembershipReturnsExisting()
    {
        var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
        var familyId = Guid.NewGuid();
        var service = new FamilyCreationService(
            new StubApplicationUserRepository(user),
            new StubFamilyRepository(new FamilyCreationPersistenceResult.Existing(familyId, ReconciledConflict: true)),
            TimeProvider.System);

        var result = await service.CreateFamilyAsync(user.ExternalIdentity, "Famiglia Bruni", CancellationToken.None);

        Assert.False(result.Created);
        Assert.True(result.ReconciledConflict);
        Assert.Equal(familyId, result.FamilyId);
    }

    [Fact]
    public async Task InvalidNameFailsBeforeWrite()
    {
        var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
        var repository = new StubFamilyRepository(new FamilyCreationPersistenceResult.Created(Guid.NewGuid()));
        var service = new FamilyCreationService(new StubApplicationUserRepository(user), repository, TimeProvider.System);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => service.CreateFamilyAsync(user.ExternalIdentity, "   ", CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.FamilyNameInvalid, exception.Code);
        Assert.Null(repository.RecordedFamily);
    }

    [Fact]
    public async Task InactiveProfileIsDenied()
    {
        var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
        user.Deactivate(DateTimeOffset.UtcNow.AddMinutes(1));
        var service = new FamilyCreationService(new StubApplicationUserRepository(user), new StubFamilyRepository(new FamilyCreationPersistenceResult.Created(Guid.NewGuid())), TimeProvider.System);

        var exception = await Assert.ThrowsAsync<BusinessAccessDeniedException>(() => service.CreateFamilyAsync(user.ExternalIdentity, "Famiglia Bruni", CancellationToken.None));

        Assert.Equal("auth.profileInactive", exception.Code);
    }

    [Fact]
    public async Task RepositoryUnavailableBecomesDependencyError()
    {
        var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
        var service = new FamilyCreationService(new StubApplicationUserRepository(user), new ThrowingFamilyRepository(), TimeProvider.System);

        var exception = await Assert.ThrowsAsync<BusinessDependencyException>(() => service.CreateFamilyAsync(user.ExternalIdentity, "Famiglia Bruni", CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.PostgreSqlUnavailable, exception.Code);
    }

    [Fact]
    public async Task CancellationTokenIsPropagated()
    {
        var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
        using var cancellationSource = new CancellationTokenSource();
        var repository = new CancellationAwareFamilyRepository(cancellationSource.Token);
        var service = new FamilyCreationService(new StubApplicationUserRepository(user), repository, TimeProvider.System);

        await service.CreateFamilyAsync(user.ExternalIdentity, "Famiglia Bruni", cancellationSource.Token);

        Assert.True(repository.ObservedCancellationTokenMatched);
    }

    private sealed class StubApplicationUserRepository(ApplicationUser user) : IApplicationUserRepository
    {
        public Task<ApplicationUser?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(user);

        public Task<ApplicationUser> GetOrCreateAsync(ExternalIdentity externalIdentity, DateTimeOffset createdAt, CancellationToken cancellationToken) => Task.FromResult(user);
    }

    private sealed class StubFamilyRepository(FamilyCreationPersistenceResult result) : IFamilyRepository
    {
        public Family? RecordedFamily { get; private set; }

        public Task<FamilyCreationPersistenceResult> CreateWithCreatorAsync(Guid applicationUserId, Family family, FamilyMembership membership, CancellationToken cancellationToken)
        {
            RecordedFamily = family;
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingFamilyRepository : IFamilyRepository
    {
        public Task<FamilyCreationPersistenceResult> CreateWithCreatorAsync(Guid applicationUserId, Family family, FamilyMembership membership, CancellationToken cancellationToken)
            => throw new RepositoryUnavailableException("db down");
    }

    private sealed class CancellationAwareFamilyRepository(CancellationToken expectedToken) : IFamilyRepository
    {
        public bool ObservedCancellationTokenMatched { get; private set; }

        public Task<FamilyCreationPersistenceResult> CreateWithCreatorAsync(Guid applicationUserId, Family family, FamilyMembership membership, CancellationToken cancellationToken)
        {
            ObservedCancellationTokenMatched = cancellationToken == expectedToken;
            return Task.FromResult<FamilyCreationPersistenceResult>(new FamilyCreationPersistenceResult.Created(family.Id));
        }
    }
}
