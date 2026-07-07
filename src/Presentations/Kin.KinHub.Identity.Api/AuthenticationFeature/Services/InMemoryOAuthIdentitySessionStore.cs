using System.Collections.Concurrent;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

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
