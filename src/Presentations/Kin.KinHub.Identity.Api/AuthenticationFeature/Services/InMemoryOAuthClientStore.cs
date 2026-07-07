using System.Collections.Concurrent;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

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
