using System.Collections.Concurrent;
using Microsoft.IdentityModel.Tokens;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

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
