namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

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
