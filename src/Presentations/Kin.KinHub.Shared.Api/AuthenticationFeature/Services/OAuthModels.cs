using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Kin.KinHub.Shared.Api.AuthenticationFeature;

public sealed class OAuthDynamicClientRegistrationRequest
{
    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("redirect_uris")]
    public string[] RedirectUris { get; set; } = [];

    [JsonPropertyName("grant_types")]
    public string[] GrantTypes { get; set; } = [];

    [JsonPropertyName("response_types")]
    public string[] ResponseTypes { get; set; } = [];

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

public sealed class OAuthAuthorizeRequest
{
    [FromQuery(Name = "response_type")]
    public string? ResponseType { get; set; }

    [FromQuery(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromQuery(Name = "scope")]
    public string? Scope { get; set; }

    [FromQuery(Name = "state")]
    public string? State { get; set; }

    [FromQuery(Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    [FromQuery(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }
}

public sealed class OAuthAuthorizeLoginRequest
{
    [FromForm(Name = "response_type")]
    public string? ResponseType { get; set; }

    [FromForm(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromForm(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }

    [FromForm(Name = "state")]
    public string? State { get; set; }

    [FromForm(Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    [FromForm(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }

    [FromForm(Name = "email")]
    public string? Email { get; set; }

    [FromForm(Name = "password")]
    public string? Password { get; set; }
}

public sealed class OAuthTokenRequest
{
    [FromForm(Name = "grant_type")]
    public string? GrantType { get; set; }

    [FromForm(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromForm(Name = "code")]
    public string? Code { get; set; }

    [FromForm(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromForm(Name = "code_verifier")]
    public string? CodeVerifier { get; set; }

    [FromForm(Name = "refresh_token")]
    public string? RefreshToken { get; set; }

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }
}

public sealed record OAuthRegisteredClient(
    string ClientId,
    string ClientName,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> GrantTypes,
    IReadOnlyList<string> ResponseTypes,
    string TokenEndpointAuthMethod,
    string Scope,
    DateTimeOffset ClientIdIssuedAt);

public sealed record OAuthAuthorizationCodeTicket(
    string Code,
    string ClientId,
    string RedirectUri,
    string Scope,
    string CodeChallenge,
    string CodeChallengeMethod,
    LoginResponse LoginResponse,
    DateTimeOffset ExpiresAtUtc);

public interface IOAuthClientStore
{
    OAuthRegisteredClient Create(OAuthDynamicClientRegistrationRequest request, string defaultScope);
    bool TryGet(string clientId, out OAuthRegisteredClient? client);
}

public interface IOAuthAuthorizationCodeStore
{
    OAuthAuthorizationCodeTicket Create(
        string clientId,
        string redirectUri,
        string scope,
        string codeChallenge,
        string codeChallengeMethod,
        LoginResponse loginResponse,
        TimeSpan lifetime);

    bool TryConsume(string code, out OAuthAuthorizationCodeTicket? ticket);
}

public sealed class InMemoryOAuthClientStore : IOAuthClientStore
{
    private readonly ConcurrentDictionary<string, OAuthRegisteredClient> _clients = new(StringComparer.Ordinal);

    public OAuthRegisteredClient Create(OAuthDynamicClientRegistrationRequest request, string defaultScope)
    {
        var clientId = $"kinhub-{Guid.NewGuid():N}";
        var issuedAt = DateTimeOffset.UtcNow;
        var client = new OAuthRegisteredClient(
            clientId,
            request.ClientName?.Trim() is { Length: > 0 } clientName ? clientName : "KinHub MCP Client",
            request.RedirectUris,
            request.GrantTypes,
            request.ResponseTypes,
            request.TokenEndpointAuthMethod ?? "none",
            string.IsNullOrWhiteSpace(request.Scope) ? defaultScope : request.Scope.Trim(),
            issuedAt);

        _clients[clientId] = client;
        return client;
    }

    public bool TryGet(string clientId, out OAuthRegisteredClient? client) =>
        _clients.TryGetValue(clientId, out client);
}

public sealed class InMemoryOAuthAuthorizationCodeStore : IOAuthAuthorizationCodeStore
{
    private readonly ConcurrentDictionary<string, OAuthAuthorizationCodeTicket> _tickets = new(StringComparer.Ordinal);

    public OAuthAuthorizationCodeTicket Create(
        string clientId,
        string redirectUri,
        string scope,
        string codeChallenge,
        string codeChallengeMethod,
        LoginResponse loginResponse,
        TimeSpan lifetime)
    {
        var code = Base64UrlEncoder.Encode(Guid.NewGuid().ToByteArray());
        var ticket = new OAuthAuthorizationCodeTicket(
            code,
            clientId,
            redirectUri,
            scope,
            codeChallenge,
            codeChallengeMethod,
            loginResponse,
            DateTimeOffset.UtcNow.Add(lifetime));

        _tickets[code] = ticket;
        return ticket;
    }

    public bool TryConsume(string code, out OAuthAuthorizationCodeTicket? ticket)
    {
        if (_tickets.TryRemove(code, out ticket)
            && ticket.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            return true;
        }

        ticket = null;
        return false;
    }
}
