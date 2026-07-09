extern alias IdentityApi;

using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Core.Domain.FamilyFeature;
using Kin.KinHub.Identity.Business.AuthenticationFeature;
using Kin.KinHub.Identity.Business.Common;
using Kin.KinHub.Identity.Domain.AuthenticationFeature;
using Kin.KinHub.Identity.Api.AuthenticationFeature;
using Kin.KinHub.Identity.Jwt.AuthenticationFeature;
using Kin.KinHub.Identity.Api.Common;
using Kin.KinHub.Identity.Api.Common.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kin.KinHub.Core.Test;

public sealed class OAuthAndAccessIntegrationTests
    : IClassFixture<OAuthAndAccessIntegrationTests.FamilyContextFactory>,
      IClassFixture<OAuthAndAccessIntegrationTests.NoFamilyContextFactory>
{
    // A fresh WebApplicationFactory per test builds (and tears down) a full ASP.NET host.
    // Doing that for every fact in this class exhausts host/timer resources on some
    // environments and hangs the test host, so the two required variants are shared
    // across the class through IClassFixture (each host is created exactly once).
    private readonly OAuthApiFactory _familyFactory;
    private readonly OAuthApiFactory _noFamilyFactory;

    public OAuthAndAccessIntegrationTests(FamilyContextFactory familyFactory, NoFamilyContextFactory noFamilyFactory)
    {
        _familyFactory = familyFactory;
        _noFamilyFactory = noFamilyFactory;
    }

    public sealed class FamilyContextFactory : OAuthApiFactory
    {
        public FamilyContextFactory()
            : base(hasFamilyContext: true)
        {
        }
    }

    public sealed class NoFamilyContextFactory : OAuthApiFactory
    {
        public NoFamilyContextFactory()
            : base(hasFamilyContext: false)
        {
        }
    }

    [Fact]
    public async Task AuthorizationServerMetadata_UsesOAuthConfiguration()
    {
        var factory = _familyFactory;
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/.well-known/oauth-authorization-server");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("http://localhost", body.GetProperty("issuer").GetString());
        Assert.Contains(OAuthScopes.Read, body.GetProperty("scopes_supported").EnumerateArray().Select(x => x.GetString()));
        Assert.Contains(OAuthScopes.Write, body.GetProperty("scopes_supported").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task AuthorizationCodeFlow_WithPkce_ReturnsBearerToken()
    {
        var factory = _familyFactory;
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeResponse = await AuthorizeAsync(client, OAuthApiFactory.ClientId, OAuthApiFactory.RedirectUri, "integration-pkce-code-verifier-rfc7636-aaaaaaaaaa");

        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        var code = QueryHelpers.ParseQuery(authorizeResponse.Headers.Location!.Query)["code"].ToString();

        var tokenResponse = await client.PostAsync(
            "/token",
            new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = OAuthApiFactory.ClientId,
                ["code"] = code,
                ["redirect_uri"] = OAuthApiFactory.RedirectUri,
                ["code_verifier"] = "integration-pkce-code-verifier-rfc7636-aaaaaaaaaa",
            }!));

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var body = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bearer", body.GetProperty("token_type").GetString());
        Assert.Equal(OAuthScopes.Read, body.GetProperty("scope").GetString());
        Assert.False(body.TryGetProperty("refresh_token", out _));
    }

    [Fact]
    public async Task AuthorizationCodeFlow_WithInvalidPkce_ReturnsInvalidGrant()
    {
        var factory = _familyFactory;
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeResponse = await AuthorizeAsync(client, OAuthApiFactory.ClientId, OAuthApiFactory.RedirectUri, "integration-pkce-code-verifier-rfc7636-aaaaaaaaaa");
        var code = QueryHelpers.ParseQuery(authorizeResponse.Headers.Location!.Query)["code"].ToString();

        var tokenResponse = await client.PostAsync(
            "/token",
            new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = OAuthApiFactory.ClientId,
                ["code"] = code,
                ["redirect_uri"] = OAuthApiFactory.RedirectUri,
                ["code_verifier"] = "wrong-pkce-code-verifier-rfc7636-bbbbbbbbbbbbbbbb",
            }!));

        Assert.Equal(HttpStatusCode.BadRequest, tokenResponse.StatusCode);
        var body = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_grant", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task AuthorizationCodeFlow_WithExistingIdentitySession_SkipsCredentialPrompt()
    {
        var factory = _familyFactory;
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var firstAuthorizeResponse = await AuthorizeAsync(client, OAuthApiFactory.ClientId, OAuthApiFactory.RedirectUri, "integration-pkce-code-verifier-rfc7636-aaaaaaaaaa");

        Assert.Equal(HttpStatusCode.Redirect, firstAuthorizeResponse.StatusCode);

        var secondAuthorizeResponse = await client.GetAsync(
            $"/authorize?response_type=code&client_id={OAuthApiFactory.ClientId}&redirect_uri={Uri.EscapeDataString(OAuthApiFactory.RedirectUri)}&scope={OAuthScopes.Read}&state=integration-state&code_challenge={ComputeCodeChallenge("integration-pkce-code-verifier-rfc7636-aaaaaaaaaa")}&code_challenge_method=S256");

        Assert.Equal(HttpStatusCode.Redirect, secondAuthorizeResponse.StatusCode);
        Assert.Contains("code=", secondAuthorizeResponse.Headers.Location!.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logout_ClearsIdentitySessionCookie()
    {
        var factory = _familyFactory;
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeResponse = await AuthorizeAsync(client, OAuthApiFactory.ClientId, OAuthApiFactory.RedirectUri, "integration-pkce-code-verifier-rfc7636-aaaaaaaaaa");

        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);

        var logoutResponse = await client.PostAsync("/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Contains(
            logoutResponse.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [],
            header => header.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FamilyContext_WhenFamilyExists_ReturnsFamilyId()
    {
        var factory = _familyFactory;
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());

        var response = await client.GetAsync("/api/access/family-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OAuthApiFactory.FamilyId, body.GetProperty("familyId").GetGuid());
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsMissingApiScope()
    {
        using var client = _familyFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new("Bearer", _familyFactory.CreateAccessToken([]));

        var response = await client.GetAsync("/api/access/family-context");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsWrongAudience()
    {
        using var client = _familyFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new("Bearer", _familyFactory.CreateAccessToken([OAuthScopes.Read], "wrong.api"));

        var response = await client.GetAsync("/api/access/family-context");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FamilyContext_WhenFamilyMissing_ReturnsFamilyRequiredProblemDetails()
    {
        var factory = _noFamilyFactory;
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());

        var response = await client.GetAsync("/api/access/family-context");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("family_required", body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task AuthMe_WhenUnauthorized_ReturnsProblemDetails()
    {
        var factory = _familyFactory;
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("authentication_required", body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task LegacyAuthLogin_IsNotExposed()
    {
        var factory = _familyFactory;
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = string.Empty,
                password = string.Empty,
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> AuthorizeAsync(HttpClient client, string clientId, string redirectUri, string codeVerifier)
    {
        var challenge = ComputeCodeChallenge(codeVerifier);
        return await client.PostAsync(
            "/authorize",
            new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["scope"] = OAuthScopes.Read,
                ["state"] = "integration-state",
                ["code_challenge"] = challenge,
                ["code_challenge_method"] = "S256",
                ["email"] = "integration@kinhub.dev",
                ["password"] = "Password123!",
                ["decision"] = "approve",
            }!));
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier));
        return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(hash);
    }
}

public class OAuthApiFactory : WebApplicationFactory<IdentityApi::Program>
{
    internal const string ClientId = "integration-client";
    internal const string RedirectUri = "http://127.0.0.1/callback";
    internal const string PostLogoutRedirectUri = "http://127.0.0.1/logout-complete";
    internal static readonly Guid UserId = Guid.Parse("5fb90fe2-31fd-4295-a81f-421fd3e8b8d2");
    internal static readonly Guid FamilyId = Guid.Parse("b5f1c687-3a8f-44cf-b75f-caa1f8c5b755");

    private readonly bool _hasFamilyContext;

    public OAuthApiFactory(bool hasFamilyContext)
    {
        _hasFamilyContext = hasFamilyContext;
    }

    public string CreateAccessToken(IReadOnlyList<string>? scopes = null, string audience = "kinhub.api")
    {
        using var scope = Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var tokenGenerator = new JwtTokenGenerator(new JwtOptions
        {
            Secret = configuration["Jwt:Secret"] is { Length: > 0 } configuredSecret
                ? configuredSecret
                : "development-only-kinhub-jwt-secret-0001",
            Issuer = configuration["Jwt:Issuer"] ?? "http://localhost",
            Audience = audience,
        });
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
            scopes ?? [OAuthScopes.Read]);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        Environment.SetEnvironmentVariable("KINHUB_OAuth__AuthorizationServerUrl", "http://localhost");
        Environment.SetEnvironmentVariable("KINHUB_ConnectionStrings__KinHub", "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub");
        Environment.SetEnvironmentVariable("KINHUB_Jwt__Issuer", "http://localhost");
        Environment.SetEnvironmentVariable("KINHUB_Jwt__Secret", "integration-only-kinhub-jwt-secret-000000000001");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KinHub"] = "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub",
                ["Jwt:Issuer"] = "http://localhost",
                ["OAuth:AuthorizationServerUrl"] = "http://localhost",
                ["OAuth:EnableDynamicClientRegistration"] = "true",
                ["OAuth:Clients:0:ClientId"] = ClientId,
                ["OAuth:Clients:0:ClientName"] = "Integration Client",
                ["OAuth:Clients:0:RedirectUris:0"] = RedirectUri,
                ["OAuth:Clients:0:RedirectUris:1"] = PostLogoutRedirectUri,
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
            services.RemoveAll<ILoginUserHandler>();
            services.RemoveAll<ILogoutUserHandler>();
            services.RemoveAll<IRefreshTokenHandler>();
            services.RemoveAll<IFamilyOwnershipService>();
            services.RemoveAll<IOAuthClientStore>();

            services.AddScoped<ILoginUserHandler>(sp => new FakeAuthenticationHandlers(sp.GetRequiredService<ITokenGenerator>()));
            services.AddScoped<ILogoutUserHandler>(sp => new FakeAuthenticationHandlers(sp.GetRequiredService<ITokenGenerator>()));
            services.AddScoped<IRefreshTokenHandler>(sp => new FakeAuthenticationHandlers(sp.GetRequiredService<ITokenGenerator>()));
            services.AddScoped<IFamilyOwnershipService>(_ => new FakeFamilyOwnershipService(_hasFamilyContext));
            services.AddSingleton<IOAuthClientStore>(new FixedOAuthClientStore());
        });
    }
}

internal sealed class FakeAuthenticationHandlers : ILoginUserHandler, ILogoutUserHandler, IRefreshTokenHandler
{
    private readonly ITokenGenerator _tokenGenerator;

    public FakeAuthenticationHandlers(ITokenGenerator tokenGenerator)
    {
        _tokenGenerator = tokenGenerator;
    }

    public Task<Result<LoginResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<LoginResponse>.Success(CreateLoginResponse()));

    Task<Result<LoginResponse>> IRefreshTokenHandler.HandleAsync(string refreshToken, CancellationToken cancellationToken) =>
        Task.FromResult(Result<LoginResponse>.Success(CreateLoginResponse()));

    Task<Result<bool>> ILogoutUserHandler.HandleAsync(string refreshToken, CancellationToken cancellationToken) =>
        Task.FromResult(Result<bool>.Success(true));

    private LoginResponse CreateLoginResponse() =>
        new()
        {
            AccessToken = _tokenGenerator.GenerateAccessToken(
                new KinUser
                {
                    Id = OAuthApiFactory.UserId,
                    Email = "integration@kinhub.dev",
                    DisplayName = "Integration User",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                },
                [],
                [OAuthScopes.Read]),
            RefreshToken = "refresh-token",
            ExpiresIn = _tokenGenerator.AccessTokenExpirySeconds,
            Email = "integration@kinhub.dev",
            DisplayName = "Integration User",
        };
}

internal sealed class FakeFamilyOwnershipService : IFamilyOwnershipService
{
    private readonly bool _hasFamilyContext;

    public FakeFamilyOwnershipService(bool hasFamilyContext)
    {
        _hasFamilyContext = hasFamilyContext;
    }

    public Task<FamilyAccessResult> GetCurrentFamilyAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_hasFamilyContext
            ? FamilyAccessResult.Success(new Family
            {
                Id = OAuthApiFactory.FamilyId,
                Name = "Kin Family",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            })
            : FamilyAccessResult.NotFound("Family not found for this user."));

    public Task<FamilyAccessResult> EnsureOwnershipAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default) =>
        GetCurrentFamilyAsync(userId, cancellationToken);
}

internal sealed class FixedOAuthClientStore : IOAuthClientStore
{
    private readonly OAuthRegisteredClient _client = new(
        OAuthApiFactory.ClientId,
        "Integration Client",
        [OAuthApiFactory.RedirectUri, OAuthApiFactory.PostLogoutRedirectUri],
        ["authorization_code"],
        ["code"],
        "none",
        OAuthScopes.Read,
        DateTimeOffset.UtcNow);

    public OAuthRegisteredClient Create(OAuthDynamicClientRegistrationRequest request, string defaultScope) => _client;

    public bool TryGet(string clientId, out OAuthRegisteredClient? client)
    {
        client = string.Equals(clientId, _client.ClientId, StringComparison.Ordinal) ? _client : null;
        return client is not null;
    }
}
