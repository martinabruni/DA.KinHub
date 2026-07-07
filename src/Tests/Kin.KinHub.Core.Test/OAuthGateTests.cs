using Kin.KinHub.Identity.Api.AuthenticationFeature;
using Kin.KinHub.Core.Api.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Additional OAuth server gate tests: authorization code replay is rejected, unregistered
/// redirect URIs are rejected, and the refresh_token grant is no longer offered.
/// </summary>
public sealed class OAuthGateTests
    : IClassFixture<OAuthAndAccessIntegrationTests.FamilyContextFactory>
{
    private readonly OAuthApiFactory _factory;

    public OAuthGateTests(OAuthAndAccessIntegrationTests.FamilyContextFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthorizationCode_Replay_IsRejected()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeResponse = await AuthorizeAsync(client, "replay-pkce-code-verifier-rfc7636-aaaaaaaaaaaaaaa");
        var code = QueryHelpers.ParseQuery(authorizeResponse.Headers.Location!.Query)["code"].ToString();

        var first = await ExchangeCodeAsync(client, code, "replay-pkce-code-verifier-rfc7636-aaaaaaaaaaaaaaa");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var replay = await ExchangeCodeAsync(client, code, "replay-pkce-code-verifier-rfc7636-aaaaaaaaaaaaaaa");
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        var body = await replay.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_grant", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Authorize_WithUnregisteredRedirectUri_IsRejected()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(
            $"/authorize?response_type=code&client_id={OAuthApiFactory.ClientId}&redirect_uri={Uri.EscapeDataString("https://evil.example.com/callback")}&scope={OAuthScopes.Read}&state=s&code_challenge={ComputeCodeChallenge("v")}&code_challenge_method=S256");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_redirect_uri", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Token_WithRefreshTokenGrant_IsUnsupported()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync(
            "/token",
            new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = OAuthApiFactory.ClientId,
                ["refresh_token"] = "whatever",
            }!));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unsupported_grant_type", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Metadata_DoesNotAdvertiseRefreshTokenGrant()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/.well-known/oauth-authorization-server");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var grants = body.GetProperty("grant_types_supported").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("authorization_code", grants);
        Assert.DoesNotContain("refresh_token", grants);
    }

    [Fact]
    public async Task Logout_WithRegisteredPostLogoutRedirectUri_Redirects()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeResponse = await AuthorizeAsync(client, "logout-pkce-code-verifier-rfc7636-aaaaaaaaaaaaaaa");
        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);

        var response = await client.PostAsync(
            $"/logout?client_id={Uri.EscapeDataString(OAuthApiFactory.ClientId)}&post_logout_redirect_uri={Uri.EscapeDataString(OAuthApiFactory.PostLogoutRedirectUri)}",
            content: null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(OAuthApiFactory.PostLogoutRedirectUri, response.Headers.Location?.ToString());
        Assert.Contains(
            response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [],
            header => header.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Logout_WithUnregisteredPostLogoutRedirectUri_DoesNotRedirect()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeResponse = await AuthorizeAsync(client, "logout-pkce-code-verifier-rfc7636-aaaaaaaaaaaaaaa");
        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);

        var response = await client.PostAsync(
            $"/logout?client_id={Uri.EscapeDataString(OAuthApiFactory.ClientId)}&post_logout_redirect_uri={Uri.EscapeDataString("https://evil.example.com/logout")}",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    private static async Task<HttpResponseMessage> AuthorizeAsync(HttpClient client, string codeVerifier)
    {
        var challenge = ComputeCodeChallenge(codeVerifier);
        return await client.PostAsync(
            "/authorize",
            new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["response_type"] = "code",
                ["client_id"] = OAuthApiFactory.ClientId,
                ["redirect_uri"] = OAuthApiFactory.RedirectUri,
                ["scope"] = OAuthScopes.Read,
                ["state"] = "gate-state",
                ["code_challenge"] = challenge,
                ["code_challenge_method"] = "S256",
                ["email"] = "integration@kinhub.dev",
                ["password"] = "Password123!",
                ["decision"] = "approve",
            }!));
    }

    private static Task<HttpResponseMessage> ExchangeCodeAsync(HttpClient client, string code, string codeVerifier) =>
        client.PostAsync(
            "/token",
            new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = OAuthApiFactory.ClientId,
                ["code"] = code,
                ["redirect_uri"] = OAuthApiFactory.RedirectUri,
                ["code_verifier"] = codeVerifier,
            }!));

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier));
        return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(hash);
    }
}
