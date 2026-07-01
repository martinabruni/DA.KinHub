using Kin.KinHub.Identity.Business.AuthenticationFeature;
using Kin.KinHub.Identity.Business.Common;
using Kin.KinHub.Identity.Domain.AuthenticationFeature;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Unit tests for identity provider link/unlink invariants: link a second provider, unlink
/// the first, and never unlink the last remaining provider.
/// </summary>
public sealed class ProviderLinkUnlinkTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task GetProviders_ReturnsLinkedProviders()
    {
        var (service, _) = CreateService(withKinHubLink: true);

        var result = await service.GetProvidersAsync(UserId);

        Assert.True(result.IsSuccess);
        var provider = Assert.Single(result.Value!);
        Assert.Equal(IdentityProviderType.KinHub, provider.Provider);
    }

    [Fact]
    public async Task Link_SecondProvider_Succeeds()
    {
        var (service, _) = CreateService(withKinHubLink: true);

        var result = await service.LinkAsync(UserId, new LinkProviderRequest
        {
            Provider = IdentityProviderType.Google,
            ExternalToken = "google-token",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value!, p => p.Provider == IdentityProviderType.Google);
    }

    [Fact]
    public async Task Link_ExistingProvider_ReturnsConflict()
    {
        var (service, _) = CreateService(withKinHubLink: true);

        var result = await service.LinkAsync(UserId, new LinkProviderRequest
        {
            Provider = IdentityProviderType.KinHub,
            Password = "another-password",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Unlink_First_WhenAnotherRemains_Succeeds()
    {
        var (service, providers) = CreateService(withKinHubLink: true);
        await service.LinkAsync(UserId, new LinkProviderRequest
        {
            Provider = IdentityProviderType.Google,
            ExternalToken = "google-token",
        });

        var result = await service.UnlinkAsync(UserId, IdentityProviderType.KinHub);

        Assert.True(result.IsSuccess);
        var remaining = Assert.Single(result.Value!);
        Assert.Equal(IdentityProviderType.Google, remaining.Provider);
        Assert.DoesNotContain(providers.Items.Values, p => p.ProviderId == (int)IdentityProviderType.KinHub);
    }

    [Fact]
    public async Task Unlink_Last_IsRejected()
    {
        var (service, _) = CreateService(withKinHubLink: true);

        var result = await service.UnlinkAsync(UserId, IdentityProviderType.KinHub);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Contains("last", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unlink_NotLinkedProvider_ReturnsNotFound()
    {
        var (service, _) = CreateService(withKinHubLink: true);

        var result = await service.UnlinkAsync(UserId, IdentityProviderType.Google);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    private static (IUserProviderService Service, InMemoryUserProviderRepository Providers) CreateService(bool withKinHubLink)
    {
        var users = new InMemoryKinUserRepository(new KinUser
        {
            Id = UserId,
            Email = "user@kinhub.dev",
            DisplayName = "User",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        var credentials = new InMemoryUserCredentialRepository();
        var providers = new InMemoryUserProviderRepository();

        if (withKinHubLink)
        {
            providers.CreateAsync(new UserProvider
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                ProviderId = (int)IdentityProviderType.KinHub,
                ProviderUserId = UserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }).GetAwaiter().GetResult();

            credentials.CreateAsync(new UserCredential
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                PasswordHash = "hash::password",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }).GetAwaiter().GetResult();
        }

        var passwordHasher = new TestPasswordHasher();

        var registry = new IdentityProviderRegistry(new IIdentityProvider[]
        {
            new KinHubPasswordIdentityProvider(users, credentials, providers, passwordHasher),
            new FakeExternalIdentityProvider(IdentityProviderType.Google, providers),
        });

        return (new UserProviderService(providers, registry), providers);
    }

    /// <summary>
    /// Minimal external provider used to exercise linking a second provider without a password.
    /// </summary>
    private sealed class FakeExternalIdentityProvider : IIdentityProvider
    {
        private readonly IUserProviderRepository _userProviderRepository;

        public FakeExternalIdentityProvider(IdentityProviderType providerType, IUserProviderRepository userProviderRepository)
        {
            ProviderType = providerType;
            _userProviderRepository = userProviderRepository;
        }

        public IdentityProviderType ProviderType { get; }

        public Task<KinUser?> AuthenticateAsync(IdentityCredential credential, CancellationToken cancellationToken = default) =>
            Task.FromResult<KinUser?>(null);

        public Task<KinUser> RegisterAsync(IdentityRegistration registration, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task LinkAsync(Guid userId, IdentityCredential credential, CancellationToken cancellationToken = default) =>
            _userProviderRepository.CreateAsync(new UserProvider
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProviderId = (int)ProviderType,
                ProviderUserId = credential.ExternalToken ?? userId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

        public async Task UnlinkAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var link = await _userProviderRepository.GetByUserAndProviderAsync(userId, (int)ProviderType);
            if (link is not null)
                await _userProviderRepository.DeleteAsync(link.Id);
        }
    }
}
