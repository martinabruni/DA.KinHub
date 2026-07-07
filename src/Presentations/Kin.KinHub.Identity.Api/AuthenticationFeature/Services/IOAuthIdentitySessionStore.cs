namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public interface IOAuthIdentitySessionStore
{
    OAuthIdentitySession Create(LoginResponse loginResponse, TimeSpan lifetime);
    bool TryGet(string sessionId, out OAuthIdentitySession? session);
    void Replace(string sessionId, LoginResponse loginResponse, TimeSpan lifetime);
    bool TryRemove(string sessionId, out OAuthIdentitySession? session);
}
