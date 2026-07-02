extern alias IdentityApi;

using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Core.Domain.FamilyFeature;
using Kin.KinHub.Identity.Business.AuthenticationFeature;
using Kin.KinHub.Identity.Domain.AuthenticationFeature;
using Kin.KinHub.Identity.Api.AuthenticationFeature;
using Kin.KinHub.Shared.Api.Common.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Gate tests for the shared family authorization policy: immediate revocation (no cache),
/// fail-closed 503 when Core is unavailable, and RFC 9457 problem details.
/// </summary>
public sealed class FamilyAuthorizationGateTests
{
    private static readonly Guid FamilyId = Guid.Parse("b5f1c687-3a8f-44cf-b75f-caa1f8c5b755");
    private static readonly Guid UserId = Guid.Parse("5fb90fe2-31fd-4295-a81f-421fd3e8b8d2");

    [Fact]
    public async Task RegisterWithoutFamily_FamilyContext_ReturnsForbidden()
    {
        using var factory = new GateFactory(() => FamilyAccessResult.NotFound("Family not found for this user."));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());

        var response = await client.GetAsync("/api/access/family-context");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("family_required", body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task CreateFamily_ThenFamilyContext_UsesSameTokenNoReissue()
    {
        // Family is resolved from the request-scoped principal (repository), never the JWT.
        // The same bearer token yields a family context once the family exists — no reissue.
        using var factory = new GateFactory(() => FamilyAccessResult.Success(new Family
        {
            Id = FamilyId,
            Name = "Kin Family",
            UserId = UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        }));
        using var client = factory.CreateClient();
        var token = factory.CreateAccessToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync("/api/access/family-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(FamilyId, body.GetProperty("familyId").GetGuid());
    }

    [Fact]
    public async Task LeaveFamily_NextRequestImmediatelyForbidden_NoCache()
    {
        // The middleware resolves the family on every request, so revoking membership takes
        // effect on the very next call with the same token.
        var hasFamily = true;
        using var factory = new GateFactory(() => hasFamily
            ? FamilyAccessResult.Success(new Family
            {
                Id = FamilyId,
                Name = "Kin Family",
                UserId = UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            })
            : FamilyAccessResult.NotFound("Family not found for this user."));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());

        var first = await client.GetAsync("/api/access/family-context");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        hasFamily = false;
        var second = await client.GetAsync("/api/access/family-context");
        Assert.Equal(HttpStatusCode.Forbidden, second.StatusCode);
    }

    [Fact]
    public async Task CoreUnavailable_FamilyEndpoint_FailsClosedWith503()
    {
        using var factory = new GateFactory(() =>
            FamilyAccessResult.ServiceUnavailable("Family context could not be resolved because Core is unavailable."));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());

        var response = await client.GetAsync("/api/access/family-context");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("family_context_unavailable", body.GetProperty("code").GetString());
    }

    private sealed class GateFactory : WebApplicationFactory<IdentityApi::Program>
    {
        internal const string ClientId = "integration-client";
        internal const string RedirectUri = "http://127.0.0.1/callback";

        private readonly Func<FamilyAccessResult> _familyResultFactory;

        public GateFactory(Func<FamilyAccessResult> familyResultFactory)
        {
            _familyResultFactory = familyResultFactory;
        }

        public string CreateAccessToken()
        {
            using var scope = Services.CreateScope();
            var tokenGenerator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();
            return tokenGenerator.GenerateAccessToken(
                new KinUser
                {
                    Id = UserId,
                    Email = "integration@kinhub.dev",
                    DisplayName = "Integration User",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                },
                [],
                [OAuthScopes.Read]);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:KinHub"] = "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub",
                    ["Jwt:Issuer"] = "http://localhost",
                    ["OAuth:AuthorizationServerUrl"] = "http://localhost",
                    ["OAuth:Clients:0:ClientId"] = ClientId,
                    ["OAuth:Clients:0:ClientName"] = "Integration Client",
                    ["OAuth:Clients:0:RedirectUris:0"] = RedirectUri,
                    ["OAuth:Clients:0:GrantTypes:0"] = "authorization_code",
                    ["OAuth:Clients:0:ResponseTypes:0"] = "code",
                    ["OAuth:Clients:0:TokenEndpointAuthMethod"] = "none",
                    ["OAuth:Clients:0:Scope"] = OAuthScopes.Read,
                    ["OAuth:SupportedScopes:0"] = OAuthScopes.Read,
                    ["OAuth:SupportedScopes:1"] = OAuthScopes.Write,
                    ["OAuth:SupportedScopes:2"] = OAuthScopes.Admin,
                    ["OAuth:DynamicClientDefaultScopes:0"] = OAuthScopes.Read,
                    ["OAuth:DynamicClientAllowedScopes:0"] = OAuthScopes.Read,
                    ["OAuth:DynamicClientAllowedScopes:1"] = OAuthScopes.Write,
                    ["OAuth:ElevatedConsentScopes:0"] = OAuthScopes.Write,
                    ["OAuth:ElevatedConsentScopes:1"] = OAuthScopes.Admin,
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFamilyOwnershipService>();
                services.AddScoped<IFamilyOwnershipService>(_ => new ConfigurableFamilyOwnershipService(_familyResultFactory));
            });
        }
    }

    private sealed class ConfigurableFamilyOwnershipService : IFamilyOwnershipService
    {
        private readonly Func<FamilyAccessResult> _resultFactory;

        public ConfigurableFamilyOwnershipService(Func<FamilyAccessResult> resultFactory)
        {
            _resultFactory = resultFactory;
        }

        public Task<FamilyAccessResult> GetCurrentFamilyAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_resultFactory());

        public Task<FamilyAccessResult> EnsureOwnershipAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_resultFactory());
    }
}
