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
