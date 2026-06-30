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

    [FromForm(Name = "decision")]
    public string? Decision { get; set; }

    [FromForm(Name = "approve_elevated_access")]
    public bool ApproveElevatedAccess { get; set; }
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

public interface IOAuthRefreshTokenScopeStore
{
    void Store(string refreshToken, string scope);
    bool TryGet(string refreshToken, out string? scope);
    void Replace(string previousRefreshToken, string nextRefreshToken, string scope);
}

public sealed class InMemoryOAuthClientStore : IOAuthClientStore
{
    private readonly ConcurrentDictionary<string, OAuthRegisteredClient> _clients = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, OAuthRegisteredClient> _configuredClients = new(StringComparer.Ordinal);
    private readonly OAuthServerOptions _options;

    public InMemoryOAuthClientStore(OAuthServerOptions options)
    {
        _options = options;
        foreach (var client in options.Clients)
        {
            if (string.IsNullOrWhiteSpace(client.ClientId) || client.RedirectUris.Length is 0)
            {
                continue;
            }

            _configuredClients[client.ClientId] = new OAuthRegisteredClient(
                client.ClientId.Trim(),
                string.IsNullOrWhiteSpace(client.ClientName) ? client.ClientId.Trim() : client.ClientName.Trim(),
                client.RedirectUris,
                client.GrantTypes,
                client.ResponseTypes,
                string.IsNullOrWhiteSpace(client.TokenEndpointAuthMethod) ? "none" : client.TokenEndpointAuthMethod.Trim(),
                client.Scope.Trim(),
                DateTimeOffset.UtcNow);
        }
    }

    public OAuthRegisteredClient Create(OAuthDynamicClientRegistrationRequest request, string defaultScope)
    {
        if (_clients.Count >= _options.MaxRegisteredClients)
        {
            throw new InvalidOperationException("Dynamic client registration capacity has been reached.");
        }

        var clientId = $"kinhub-{Guid.NewGuid():N}";
        var issuedAt = DateTimeOffset.UtcNow;
        var client = new OAuthRegisteredClient(
            clientId,
            request.ClientName?.Trim() is { Length: > 0 } clientName ? clientName : "KinHub Client",
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
        _configuredClients.TryGetValue(clientId, out client)
        || _clients.TryGetValue(clientId, out client);
}

public sealed class InMemoryOAuthAuthorizationCodeStore : IOAuthAuthorizationCodeStore
{
    private readonly ConcurrentDictionary<string, OAuthAuthorizationCodeTicket> _tickets = new(StringComparer.Ordinal);
    private readonly OAuthServerOptions _options;

    public InMemoryOAuthAuthorizationCodeStore(OAuthServerOptions options)
    {
        _options = options;
    }

    public OAuthAuthorizationCodeTicket Create(
        string clientId,
        string redirectUri,
        string scope,
        string codeChallenge,
        string codeChallengeMethod,
        LoginResponse loginResponse,
        TimeSpan lifetime)
    {
        CleanupExpiredTickets();
        if (_tickets.Count >= _options.MaxAuthorizationCodes)
        {
            throw new InvalidOperationException("Authorization code capacity has been reached.");
        }

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

    private void CleanupExpiredTickets()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _tickets)
        {
            if (item.Value.ExpiresAtUtc <= now)
            {
                _tickets.TryRemove(item.Key, out _);
            }
        }
    }
}

public sealed class InMemoryOAuthRefreshTokenScopeStore : IOAuthRefreshTokenScopeStore
{
    private readonly ConcurrentDictionary<string, string> _scopes = new(StringComparer.Ordinal);
    private readonly OAuthServerOptions _options;

    public InMemoryOAuthRefreshTokenScopeStore(OAuthServerOptions options)
    {
        _options = options;
    }

    public void Store(string refreshToken, string scope)
    {
        if (_scopes.Count >= _options.MaxScopedRefreshTokens)
        {
            throw new InvalidOperationException("Scoped refresh token capacity has been reached.");
        }

        _scopes[refreshToken] = scope;
    }

    public bool TryGet(string refreshToken, out string? scope) =>
        _scopes.TryGetValue(refreshToken, out scope);

    public void Replace(string previousRefreshToken, string nextRefreshToken, string scope)
    {
        _scopes.TryRemove(previousRefreshToken, out _);
        Store(nextRefreshToken, scope);
    }
}

public sealed record OAuthIdentitySession(
    string SessionId,
    string RefreshToken,
    string Email,
    string? DisplayName,
    DateTimeOffset ExpiresAtUtc);

public interface IOAuthIdentitySessionStore
{
    OAuthIdentitySession Create(LoginResponse loginResponse, TimeSpan lifetime);
    bool TryGet(string sessionId, out OAuthIdentitySession? session);
    void Replace(string sessionId, LoginResponse loginResponse, TimeSpan lifetime);
    bool TryRemove(string sessionId, out OAuthIdentitySession? session);
}

public sealed class InMemoryOAuthIdentitySessionStore : IOAuthIdentitySessionStore
{
    private readonly ConcurrentDictionary<string, OAuthIdentitySession> _sessions = new(StringComparer.Ordinal);
    private readonly OAuthServerOptions _options;

    public InMemoryOAuthIdentitySessionStore(OAuthServerOptions options)
    {
        _options = options;
    }

    public OAuthIdentitySession Create(LoginResponse loginResponse, TimeSpan lifetime)
    {
        CleanupExpiredSessions();
        if (_sessions.Count >= _options.MaxIdentitySessions)
        {
            throw new InvalidOperationException("Identity session capacity has been reached.");
        }

        var session = CreateSession(Guid.NewGuid().ToString("N"), loginResponse, lifetime);
        _sessions[session.SessionId] = session;
        return session;
    }

    public bool TryGet(string sessionId, out OAuthIdentitySession? session)
    {
        if (_sessions.TryGetValue(sessionId, out session)
            && session.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            return true;
        }

        if (session is not null)
        {
            _sessions.TryRemove(sessionId, out _);
        }

        session = null;
        return false;
    }

    public void Replace(string sessionId, LoginResponse loginResponse, TimeSpan lifetime)
    {
        _sessions[sessionId] = CreateSession(sessionId, loginResponse, lifetime);
    }

    public bool TryRemove(string sessionId, out OAuthIdentitySession? session) =>
        _sessions.TryRemove(sessionId, out session);

    private static OAuthIdentitySession CreateSession(string sessionId, LoginResponse loginResponse, TimeSpan lifetime) =>
        new(
            sessionId,
            loginResponse.RefreshToken,
            loginResponse.Email,
            loginResponse.DisplayName,
            DateTimeOffset.UtcNow.Add(lifetime));

    private void CleanupExpiredSessions()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var session in _sessions)
        {
            if (session.Value.ExpiresAtUtc <= now)
            {
                _sessions.TryRemove(session.Key, out _);
            }
        }
    }
}
