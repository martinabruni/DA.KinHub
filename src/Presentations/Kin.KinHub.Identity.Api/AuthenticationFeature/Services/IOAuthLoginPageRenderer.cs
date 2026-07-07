namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public interface IOAuthLoginPageRenderer
{
    string Render(
        OAuthAuthorizeRequest request,
        OAuthRegisteredClient client,
        string scope,
        string authorizationServerUrl,
        string registrationUiUrl,
        string? errorMessage = null);
}
