using Kin.KinHub.Core.Business.Common;
using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Identity.Business.AuthenticationFeature;
using Kin.KinHub.Identity.Domain.AuthenticationFeature;
using Kin.KinHub.Shared.Api.Common.Mcp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Kin.KinHub.Core.Test;

public sealed class McpIntegrationTests : IClassFixture<McpApiFactory>
{
    private readonly McpApiFactory _factory;

    public McpIntegrationTests(McpApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Initialize_Anonymous_ReturnsUnauthorizedChallenge()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/mcp", CreateInitializePayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(
            response.Headers.WwwAuthenticate,
            header => header.Parameter?.Contains(".well-known/oauth-protected-resource", StringComparison.OrdinalIgnoreCase) is true);
    }

    [Fact]
    public async Task Initialize_Authenticated_DoesNotReturnSessionHeader_AndReturnsCapabilities()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await _factory.CreateOAuthAccessTokenAsync(_factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })));

        var response = await client.PostAsJsonAsync("/api/v1/mcp", CreateInitializePayload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Mcp-Session-Id"));

        var body = await response.ReadAsJsonElementAsync();
        Assert.Equal("2025-03-26", body.GetProperty("result").GetProperty("protocolVersion").GetString());
        Assert.Equal("Kin.KinHub Shared API", body.GetProperty("result").GetProperty("serverInfo").GetProperty("name").GetString());
        Assert.True(body.GetProperty("result").GetProperty("capabilities").TryGetProperty("tools", out _));
    }

    [Fact]
    public async Task ToolsList_Authenticated_ReturnsProtectedToolCatalog()
    {
        await using var client = await _factory.CreateMcpClientAsync();

        var tools = await client.ListToolsAsync();
        var toolNames = tools.Select(tool => tool.Name).OrderBy(name => name).ToArray();

        Assert.Contains("auth.account.get", toolNames);
        Assert.Contains("family.get", toolNames);
        Assert.Contains("recipe.get", toolNames);
        Assert.Contains("recipe-assistant.parse", toolNames);
        Assert.DoesNotContain("auth.account.update-email", toolNames);
        Assert.DoesNotContain("auth.account.delete", toolNames);
        Assert.DoesNotContain("auth.login", toolNames);
        Assert.DoesNotContain("auth.refresh", toolNames);
        Assert.DoesNotContain("auth.logout", toolNames);
        Assert.DoesNotContain("auth.register", toolNames);
    }

    [Fact]
    public async Task FamilyGet_ReturnsToolPayloadWhenAuthenticated()
    {
        await using var client = await _factory.CreateMcpClientAsync();

        var result = await client.CallToolAsync(
            "family.get",
            new Dictionary<string, object?>());

        Assert.False(result.IsError ?? false);

        var payload = JsonDocument.Parse(Assert.Single(result.Content.OfType<TextContentBlock>()).Text);
        Assert.Equal("Kin Family", payload.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task AuthorizationServerMetadataEndpoint_ReturnsOAuthDiscoveryDocument()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/.well-known/oauth-authorization-server");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("http://localhost", body.GetProperty("issuer").GetString());
        Assert.Equal("http://localhost/authorize", body.GetProperty("authorization_endpoint").GetString());
        Assert.Equal("http://localhost/token", body.GetProperty("token_endpoint").GetString());
        Assert.Equal("http://localhost/register", body.GetProperty("registration_endpoint").GetString());
        Assert.Contains("authorization_code", body.GetProperty("grant_types_supported").EnumerateArray().Select(x => x.GetString()));
        Assert.Contains("S256", body.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(x => x.GetString()));
        Assert.Contains(McpScopes.Read, body.GetProperty("scopes_supported").EnumerateArray().Select(x => x.GetString()));
        Assert.Contains(McpScopes.Write, body.GetProperty("scopes_supported").EnumerateArray().Select(x => x.GetString()));
        Assert.Contains(McpScopes.Admin, body.GetProperty("scopes_supported").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task ProtectedResourceMetadataEndpoint_ReturnsMcpResourceDocument()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/.well-known/oauth-protected-resource");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("KinHub MCP", body.GetProperty("resource_name").GetString());
        Assert.Equal("http://localhost", body.GetProperty("resource").GetString());
        Assert.Contains("http://localhost", body.GetProperty("authorization_servers").EnumerateArray().Select(server => server.GetString()));
        Assert.Contains(McpScopes.Read, body.GetProperty("scopes_supported").EnumerateArray().Select(scope => scope.GetString()));
        Assert.Contains(McpScopes.Write, body.GetProperty("scopes_supported").EnumerateArray().Select(scope => scope.GetString()));
        Assert.Contains(McpScopes.Admin, body.GetProperty("scopes_supported").EnumerateArray().Select(scope => scope.GetString()));
    }

    [Fact]
    public async Task TokenEndpoint_AuthorizationCodeFlow_ReturnsBearerAndRefreshTokens()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var tokenResponse = await _factory.CreateOAuthTokenResponseAsync(client);

        Assert.Equal("Bearer", tokenResponse.GetProperty("token_type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.GetProperty("access_token").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.GetProperty("refresh_token").GetString()));
        Assert.Equal(McpScopes.Read, tokenResponse.GetProperty("scope").GetString());
    }

    [Fact]
    public async Task RegisterClient_RequestingAdminScope_ReturnsInvalidScope()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.PostAsJsonAsync(
            "/register",
            new
            {
                client_name = "integration-test-client",
                redirect_uris = new[] { "http://127.0.0.1/callback" },
                grant_types = new[] { "authorization_code", "refresh_token" },
                response_types = new[] { "code" },
                token_endpoint_auth_method = "none",
                scope = $"{McpScopes.Read} {McpScopes.Write} {McpScopes.Admin}",
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_scope", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task TokenEndpoint_WriteScopeRequiresExplicitConsent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await _factory.AuthorizeOAuthClientAsync(client, $"{McpScopes.Read} {McpScopes.Write}", approveElevatedAccess: false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("explicitly approve elevated MCP access", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TokenEndpoint_WriteScopeWithConsent_ReturnsWriteScopedToken()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var tokenResponse = await _factory.CreateOAuthTokenResponseAsync(client, $"{McpScopes.Read} {McpScopes.Write}", approveElevatedAccess: true);

        Assert.Equal($"{McpScopes.Read} {McpScopes.Write}", tokenResponse.GetProperty("scope").GetString());
    }

    [Fact]
    public async Task Authorize_Deny_ReturnsAccessDeniedRedirect()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await _factory.AuthorizeOAuthClientAsync(client, McpScopes.Read, decision: "deny");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);

        var callbackQuery = QueryHelpers.ParseQuery(location!.Query);
        Assert.Equal("access_denied", callbackQuery["error"].ToString());
        Assert.Equal("integration-state", callbackQuery["state"].ToString());
    }

    [Fact]
    public async Task Health_OnAzureHost_ReturnsOk()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://kinhub-dev-app.azurewebsites.net"),
        });

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LoginPreflight_WithAllowedSwaOrigin_ReturnsCorsHeaders()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://kinhub-dev-app.azurewebsites.net"),
        });

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "https://orange-plant-0cdfb6b03.7.azurestaticapps.net");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        if (response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins))
        {
            Assert.Equal("https://orange-plant-0cdfb6b03.7.azurestaticapps.net", origins.Single());
        }
    }

    private static object CreateInitializePayload() => new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "initialize",
        @params = new
        {
            protocolVersion = "2025-03-26",
            capabilities = new { },
            clientInfo = new
            {
                name = "integration-test",
                version = "1.0.0",
            },
        },
    };
}

public sealed class McpApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KinHub"] = "Host=localhost;Database=kinhub;Username=test;Password=test",
                ["Jwt:Secret"] = "abcdefghijklmnopqrstuvwxyz123456",
                ["Jwt:Issuer"] = "kinhub-tests",
                ["OpenAi:Endpoint"] = "https://localhost/",
                ["OpenAi:ApiKey"] = "test-key",
                ["Cors:AllowAnyOrigin"] = "false",
                ["Cors:AllowedOrigins:0"] = "https://orange-plant-0cdfb6b03.7.azurestaticapps.net",
                ["Mcp:AuthorizationServerUrl"] = "http://localhost",
                ["Mcp:EnableDynamicClientRegistration"] = "true",
                ["Mcp:SupportedScopes:0"] = McpScopes.Read,
                ["Mcp:SupportedScopes:1"] = McpScopes.Write,
                ["Mcp:SupportedScopes:2"] = McpScopes.Admin,
                ["Mcp:DynamicClientDefaultScopes:0"] = McpScopes.Read,
                ["Mcp:DynamicClientAllowedScopes:0"] = McpScopes.Read,
                ["Mcp:DynamicClientAllowedScopes:1"] = McpScopes.Write,
                ["Mcp:ElevatedConsentScopes:0"] = McpScopes.Write,
                ["Mcp:ElevatedConsentScopes:1"] = McpScopes.Admin,
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFamilyService>();
            services.RemoveAll<IAuthenticationService>();
            services.RemoveAll<IRegisterUserHandler>();
            services.RemoveAll<IGetCurrentUserHandler>();
            services.RemoveAll<IUpdateUserEmailHandler>();
            services.RemoveAll<IUpdateUserPasswordHandler>();
            services.RemoveAll<IDeleteUserHandler>();
            services.AddScoped<IFamilyService, FakeFamilyService>();
            services.AddSingleton<FakeAuthenticationState>();
            services.AddScoped<IAuthenticationService, FakeAuthenticationService>();
            services.AddScoped<IRegisterUserHandler, FakeRegisterUserHandler>();
            services.AddScoped<IGetCurrentUserHandler, FakeGetCurrentUserHandler>();
            services.AddScoped<IUpdateUserEmailHandler, FakeUpdateUserEmailHandler>();
            services.AddScoped<IUpdateUserPasswordHandler, FakeUpdateUserPasswordHandler>();
            services.AddScoped<IDeleteUserHandler, FakeDeleteUserHandler>();
        });
    }

    public async Task<McpClient> CreateMcpClientAsync(string? scope = null, bool approveElevatedAccess = false)
    {
        var httpClient = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var accessToken = await CreateOAuthAccessTokenAsync(httpClient, scope, approveElevatedAccess);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(httpClient.BaseAddress!, "/api/v1/mcp"),
            },
            httpClient);

        return await McpClient.CreateAsync(transport);
    }

    public async Task<string> CreateOAuthAccessTokenAsync(HttpClient? client = null, string? scope = null, bool approveElevatedAccess = false)
    {
        var httpClient = client ?? CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var tokenResponse = await CreateOAuthTokenResponseAsync(httpClient, scope, approveElevatedAccess);
        return tokenResponse.GetProperty("access_token").GetString()!;
    }

    public async Task<HttpResponseMessage> AuthorizeOAuthClientAsync(HttpClient client, string? scope = null, bool approveElevatedAccess = false, string decision = "approve")
    {
        var registrationResponse = await client.PostAsJsonAsync(
            "/register",
            new
            {
                client_name = "integration-test-client",
                redirect_uris = new[] { "http://127.0.0.1/callback" },
                grant_types = new[] { "authorization_code", "refresh_token" },
                response_types = new[] { "code" },
                token_endpoint_auth_method = "none",
                scope,
            });

        registrationResponse.EnsureSuccessStatusCode();
        var registrationBody = await registrationResponse.Content.ReadFromJsonAsync<JsonElement>();
        var clientId = registrationBody.GetProperty("client_id").GetString()!;
        const string redirectUri = "http://127.0.0.1/callback";
        const string codeVerifier = "integration-test-code-verifier-0123456789";
        var authorizeQuery = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["state"] = "integration-state",
            ["code_challenge"] = ComputeCodeChallenge(codeVerifier),
            ["code_challenge_method"] = "S256",
        };
        if (!string.IsNullOrWhiteSpace(scope))
        {
            authorizeQuery["scope"] = scope;
        }

        var authorizeUri = QueryHelpers.AddQueryString("/authorize", authorizeQuery);
        var form = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["state"] = "integration-state",
            ["code_challenge"] = ComputeCodeChallenge(codeVerifier),
            ["code_challenge_method"] = "S256",
            ["email"] = FakeAuthenticationService.Email,
            ["password"] = FakeAuthenticationService.Password,
            ["decision"] = decision,
        };
        if (!string.IsNullOrWhiteSpace(scope))
        {
            form["scope"] = scope;
        }

        if (approveElevatedAccess)
        {
            form["approve_elevated_access"] = "true";
        }

        return await client.PostAsync(authorizeUri, new FormUrlEncodedContent(form));
    }

    public async Task<JsonElement> CreateOAuthTokenResponseAsync(HttpClient client, string? scope = null, bool approveElevatedAccess = false)
    {
        var authorizeResponse = await AuthorizeOAuthClientAsync(client, scope, approveElevatedAccess);
        const string codeVerifier = "integration-test-code-verifier-0123456789";

        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        var location = authorizeResponse.Headers.Location;
        Assert.NotNull(location);

        var callbackQuery = QueryHelpers.ParseQuery(location!.Query);
        var authorizeQuery = QueryHelpers.ParseQuery(authorizeResponse.RequestMessage!.RequestUri!.Query);
        var code = callbackQuery["code"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(code));
        Assert.Equal("integration-state", callbackQuery["state"].ToString());

        var tokenEndpointResponse = await client.PostAsync(
            "/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = authorizeQuery["client_id"].ToString(),
                ["code"] = code,
                ["redirect_uri"] = authorizeQuery["redirect_uri"].ToString(),
                ["code_verifier"] = codeVerifier,
            }));

        tokenEndpointResponse.EnsureSuccessStatusCode();
        return (await tokenEndpointResponse.Content.ReadFromJsonAsync<JsonElement>()).Clone();
    }

    private static string ComputeCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlTextEncoder.Encode(hash);
    }
}

internal static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostAsJsonAsync(
        this HttpClient client,
        string requestUri,
        object payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        return await client.SendAsync(request);
    }

    public static async Task<JsonElement> ReadAsJsonElementAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.Content.Headers.ContentType?.MediaType == "text/event-stream")
        {
            var data = content
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                .Select(line => line["data:".Length..].Trim())
                .First();

            return JsonDocument.Parse(data).RootElement.Clone();
        }

        return JsonDocument.Parse(content).RootElement.Clone();
    }
}

internal sealed class FakeFamilyService : IFamilyService
{
    public Task<Result<AddFamilyMemberResponse>> AddFamilyMemberAsync(Guid familyId, AddFamilyMemberRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<CreateFamilyResponse>> CreateFamilyAsync(CreateFamilyRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<bool>> DeleteFamilyAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<bool>> DeleteFamilyMemberAsync(Guid familyId, Guid memberId, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<FamilyDetailResponse>> GetFamilyAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<FamilyDetailResponse>.Success(new FamilyDetailResponse
        {
            Id = Guid.Parse("1f440eb4-1b6d-4e0e-9eb2-0bdb4a3ef624"),
            Name = "Kin Family",
            Members =
            [
                new FamilyMemberDto
                {
                    Id = Guid.Parse("3a3e6553-429d-4796-bc90-dd4106d80e61"),
                    Name = "Martina",
                },
            ],
        }));

    public Task<Result<UpdateFamilyResponse>> UpdateFamilyAsync(Guid familyId, UpdateFamilyRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<UpdateFamilyMemberResponse>> UpdateFamilyMemberAsync(Guid familyId, Guid memberId, UpdateFamilyMemberRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class FakeAuthenticationState
{
    private readonly ConcurrentDictionary<string, Guid> _tokens = new(StringComparer.Ordinal);

    public string IssueRefreshToken(Guid userId)
    {
        var refreshToken = $"refresh-{Guid.NewGuid():N}";
        _tokens[refreshToken] = userId;
        return refreshToken;
    }

    public bool TryConsumeRefreshToken(string refreshToken, out Guid userId)
    {
        if (_tokens.TryRemove(refreshToken, out userId))
            return true;

        userId = Guid.Empty;
        return false;
    }

    public bool RevokeRefreshToken(string refreshToken) =>
        _tokens.TryRemove(refreshToken, out _);
}

internal sealed class FakeAuthenticationService : IAuthenticationService
{
    internal static readonly Guid UserId = Guid.Parse("08cbaf25-9470-4c33-8542-9a81151ffb26");
    internal const string Email = "member@kinhub.dev";
    internal const string Password = "test-password";

    private readonly ITokenGenerator _tokenGenerator;
    private readonly FakeAuthenticationState _state;

    public FakeAuthenticationService(ITokenGenerator tokenGenerator, FakeAuthenticationState state)
    {
        _tokenGenerator = tokenGenerator;
        _state = state;
    }

    public Task<Kin.KinHub.Identity.Business.Common.Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Kin.KinHub.Identity.Business.Common.Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.Email, Email, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(request.Password, Password, StringComparison.Ordinal))
        {
            return Task.FromResult(Kin.KinHub.Identity.Business.Common.Result<LoginResponse>.Unauthorized("Invalid email or password."));
        }

        return Task.FromResult(Kin.KinHub.Identity.Business.Common.Result<LoginResponse>.Success(CreateLoginResponse()));
    }

    public Task<Kin.KinHub.Identity.Business.Common.Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!_state.TryConsumeRefreshToken(refreshToken, out var userId) || userId != UserId)
        {
            return Task.FromResult(Kin.KinHub.Identity.Business.Common.Result<LoginResponse>.Unauthorized("Invalid or expired refresh token."));
        }

        return Task.FromResult(Kin.KinHub.Identity.Business.Common.Result<LoginResponse>.Success(CreateLoginResponse()));
    }

    public Task<Kin.KinHub.Identity.Business.Common.Result<bool>> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(_state.RevokeRefreshToken(refreshToken)
            ? Kin.KinHub.Identity.Business.Common.Result<bool>.Success(true)
            : Kin.KinHub.Identity.Business.Common.Result<bool>.NotFound("Refresh token not found."));

    public Task<Kin.KinHub.Identity.Business.Common.Result<UserProfileResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Kin.KinHub.Identity.Business.Common.Result<UserProfileResponse>.Success(new UserProfileResponse
        {
            UserId = UserId,
            Email = Email,
            DisplayName = "Martina",
        }));

    public Task<Kin.KinHub.Identity.Business.Common.Result<bool>> UpdateUserEmailAsync(Guid userId, UpdateUserEmailRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Kin.KinHub.Identity.Business.Common.Result<bool>> UpdateUserPasswordAsync(Guid userId, UpdateUserPasswordRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Kin.KinHub.Identity.Business.Common.Result<bool>> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    private LoginResponse CreateLoginResponse()
    {
        var user = new KinUser
        {
            Id = UserId,
            Email = Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        return new LoginResponse
        {
            AccessToken = _tokenGenerator.GenerateAccessToken(user, []),
            RefreshToken = _state.IssueRefreshToken(UserId),
            ExpiresIn = _tokenGenerator.AccessTokenExpirySeconds,
            Email = Email,
            DisplayName = "Martina",
        };
    }
}

internal sealed class FakeRegisterUserHandler : IRegisterUserHandler
{
    private readonly IAuthenticationService _authenticationService;

    public FakeRegisterUserHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<Kin.KinHub.Identity.Business.Common.Result<RegisterResponse>> HandleAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        _authenticationService.RegisterAsync(request, cancellationToken);
}

internal sealed class FakeGetCurrentUserHandler : IGetCurrentUserHandler
{
    private readonly IAuthenticationService _authenticationService;

    public FakeGetCurrentUserHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<Kin.KinHub.Identity.Business.Common.Result<UserProfileResponse>> HandleAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _authenticationService.GetCurrentUserAsync(userId, cancellationToken);
}

internal sealed class FakeUpdateUserEmailHandler : IUpdateUserEmailHandler
{
    private readonly IAuthenticationService _authenticationService;

    public FakeUpdateUserEmailHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<Kin.KinHub.Identity.Business.Common.Result<bool>> HandleAsync(Guid userId, UpdateUserEmailRequest request, CancellationToken cancellationToken = default) =>
        _authenticationService.UpdateUserEmailAsync(userId, request, cancellationToken);
}

internal sealed class FakeUpdateUserPasswordHandler : IUpdateUserPasswordHandler
{
    private readonly IAuthenticationService _authenticationService;

    public FakeUpdateUserPasswordHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<Kin.KinHub.Identity.Business.Common.Result<bool>> HandleAsync(Guid userId, UpdateUserPasswordRequest request, CancellationToken cancellationToken = default) =>
        _authenticationService.UpdateUserPasswordAsync(userId, request, cancellationToken);
}

internal sealed class FakeDeleteUserHandler : IDeleteUserHandler
{
    private readonly IAuthenticationService _authenticationService;

    public FakeDeleteUserHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<Kin.KinHub.Identity.Business.Common.Result<bool>> HandleAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _authenticationService.DeleteUserAsync(userId, cancellationToken);
}
