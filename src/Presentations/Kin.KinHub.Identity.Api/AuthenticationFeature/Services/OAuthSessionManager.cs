namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public interface IOAuthSessionManager
{
    bool TryGetIdentitySession(HttpRequest request, HttpResponse response, out OAuthIdentitySession? session);
    bool TryGetIdentitySessionId(HttpRequest request, out string sessionId);
    void WriteIdentitySessionCookie(HttpRequest request, HttpResponse response, OAuthIdentitySession session);
    void DeleteIdentitySessionCookie(HttpRequest request, HttpResponse response);
    Task<LoginResponse> RehydrateLoginResponseAsync(
        HttpRequest request,
        HttpResponse response,
        OAuthIdentitySession session,
        CancellationToken cancellationToken);
}

public sealed class OAuthSessionManager : IOAuthSessionManager
{
    private const string SessionCookiePath = "/";

    private readonly IIdentitySessionService _sessionService;
    private readonly IOAuthIdentitySessionStore _identitySessionStore;
    private readonly OAuthServerOptions _oauthOptions;

    public OAuthSessionManager(
        IIdentitySessionService sessionService,
        IOAuthIdentitySessionStore identitySessionStore,
        OAuthServerOptions oauthOptions)
    {
        _sessionService = sessionService;
        _identitySessionStore = identitySessionStore;
        _oauthOptions = oauthOptions;
    }

    public bool TryGetIdentitySession(HttpRequest request, HttpResponse response, out OAuthIdentitySession? session)
    {
        session = null;
        if (!TryGetIdentitySessionId(request, out var sessionId))
        {
            return false;
        }

        if (!_identitySessionStore.TryGet(sessionId, out var storedSession) || storedSession is null)
        {
            DeleteIdentitySessionCookie(request, response);
            return false;
        }

        session = storedSession;
        return session is not null;
    }

    public bool TryGetIdentitySessionId(HttpRequest request, out string sessionId)
    {
        sessionId = request.Cookies[_oauthOptions.SessionCookieName] ?? string.Empty;
        return !string.IsNullOrWhiteSpace(sessionId);
    }

    public void WriteIdentitySessionCookie(HttpRequest request, HttpResponse response, OAuthIdentitySession session)
    {
        response.Cookies.Append(
            _oauthOptions.SessionCookieName,
            session.SessionId,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = request.IsHttps,
                Path = SessionCookiePath,
                Expires = session.ExpiresAtUtc,
            });
    }

    public void DeleteIdentitySessionCookie(HttpRequest request, HttpResponse response)
    {
        response.Cookies.Delete(
            _oauthOptions.SessionCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = request.IsHttps,
                Path = SessionCookiePath,
            });
    }

    public async Task<LoginResponse> RehydrateLoginResponseAsync(
        HttpRequest request,
        HttpResponse response,
        OAuthIdentitySession session,
        CancellationToken cancellationToken)
    {
        var refreshResult = await _sessionService.RefreshAsync(session.RefreshToken, cancellationToken);

        if (!refreshResult.IsSuccess || refreshResult.Value is null)
        {
            _identitySessionStore.TryRemove(session.SessionId, out _);
            throw new UnauthorizedAccessException("Stored identity session is no longer valid.");
        }

        _identitySessionStore.Replace(
            session.SessionId,
            refreshResult.Value,
            TimeSpan.FromHours(_oauthOptions.SessionLifetimeHours));

        WriteIdentitySessionCookie(
            request,
            response,
            new OAuthIdentitySession(
                session.SessionId,
                refreshResult.Value.RefreshToken,
                refreshResult.Value.Email,
                refreshResult.Value.DisplayName,
                DateTimeOffset.UtcNow.AddHours(_oauthOptions.SessionLifetimeHours)));

        return refreshResult.Value;
    }
}
