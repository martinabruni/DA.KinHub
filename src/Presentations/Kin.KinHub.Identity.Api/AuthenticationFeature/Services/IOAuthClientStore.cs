namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public interface IOAuthClientStore
{
    OAuthRegisteredClient Create(OAuthDynamicClientRegistrationRequest request, string defaultScope);
    bool TryGet(string clientId, out OAuthRegisteredClient? client);
}
