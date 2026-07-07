namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public interface IOAuthTokenIssuer
{
    object CreateScopedTokenResponse(LoginResponse response, string scope);
}
