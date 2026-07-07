using System.Collections.Concurrent;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

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
