extern alias IdentityApi;

using Kin.KinHub.Identity.Business.AuthenticationFeature;
using Kin.KinHub.Identity.Business.Common;
using Kin.KinHub.Identity.Domain.AuthenticationFeature;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Verifies the public Identity API contract for linked providers. The business-layer
/// invariants already have unit coverage; these tests prove the authenticated HTTP surface
/// exposes list/link/unlink with the expected status codes and payload shape.
/// </summary>
public sealed class AuthMeProvidersApiTests
{
    [Fact]
    public async Task GetProviders_ReturnsLinkedProviders()
    {
        using var factory = new ProviderApiFactory(new StubUserProviderService(
            getProviders: Result<IReadOnlyList<LinkedProviderResponse>>.Success([
                new LinkedProviderResponse
                {
                    Provider = IdentityProviderType.KinHub,
                    ProviderName = "KinHub",
                    LinkedAt = DateTime.UtcNow,
                },
            ])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());

        var response = await client.GetAsync("/api/auth/me/providers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var provider = Assert.Single(body.EnumerateArray());
        Assert.Equal("KinHub", provider.GetProperty("providerName").GetString());
    }

    [Fact]
    public async Task LinkProvider_WhenAlreadyLinked_ReturnsConflictProblemDetails()
    {
        using var factory = new ProviderApiFactory(new StubUserProviderService(
            linkResult: Result<IReadOnlyList<LinkedProviderResponse>>.Conflict(
                "The 'KinHub' provider is already linked to this account.")));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());

        var response = await client.PostAsJsonAsync("/api/auth/me/providers", new
        {
            provider = IdentityProviderType.KinHub,
            password = "Password123!",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("conflict", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnlinkProvider_WhenLastRemaining_ReturnsValidationProblemDetails()
    {
        using var factory = new ProviderApiFactory(new StubUserProviderService(
            unlinkResult: Result<IReadOnlyList<LinkedProviderResponse>>.ValidationError(
                "Cannot unlink the last remaining identity provider.")));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());

        var response = await client.DeleteAsync($"/api/auth/me/providers/{IdentityProviderType.KinHub}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", body.GetProperty("code").GetString());
        Assert.Contains("last", body.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ProviderApiFactory : OAuthApiFactory
    {
        private readonly IUserProviderService _userProviderService;

        public ProviderApiFactory(IUserProviderService userProviderService)
            : base(hasFamilyContext: true)
        {
            _userProviderService = userProviderService;
        }

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserProviderService>();
                services.AddScoped(_ => _userProviderService);
            });
        }
    }

    private sealed class StubUserProviderService : IUserProviderService
    {
        private readonly Result<IReadOnlyList<LinkedProviderResponse>> _getProviders;
        private readonly Result<IReadOnlyList<LinkedProviderResponse>> _linkResult;
        private readonly Result<IReadOnlyList<LinkedProviderResponse>> _unlinkResult;

        public StubUserProviderService(
            Result<IReadOnlyList<LinkedProviderResponse>>? getProviders = null,
            Result<IReadOnlyList<LinkedProviderResponse>>? linkResult = null,
            Result<IReadOnlyList<LinkedProviderResponse>>? unlinkResult = null)
        {
            _getProviders = getProviders ?? Result<IReadOnlyList<LinkedProviderResponse>>.Success([]);
            _linkResult = linkResult ?? Result<IReadOnlyList<LinkedProviderResponse>>.Success([]);
            _unlinkResult = unlinkResult ?? Result<IReadOnlyList<LinkedProviderResponse>>.Success([]);
        }

        public Task<Result<IReadOnlyList<LinkedProviderResponse>>> GetProvidersAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_getProviders);

        public Task<Result<IReadOnlyList<LinkedProviderResponse>>> LinkAsync(Guid userId, LinkProviderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(_linkResult);

        public Task<Result<IReadOnlyList<LinkedProviderResponse>>> UnlinkAsync(Guid userId, IdentityProviderType provider, CancellationToken cancellationToken = default) =>
            Task.FromResult(_unlinkResult);
    }
}
