using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public interface IOAuthRequestValidator
{
    bool TryValidateAuthorizationRequest(
        ControllerBase controller,
        OAuthAuthorizeRequest request,
        out OAuthRegisteredClient? client,
        out string scope,
        out IActionResult? errorResult);

    bool TryResolveDynamicClientScope(
        ControllerBase controller,
        string? requestedScope,
        out string resolvedScope,
        out IActionResult? errorResult);

    IActionResult RedirectOAuthError(string redirectUri, string? state, string error, string description);
    string BuildAuthorizationSuccessRedirect(string redirectUri, string? state, string code);
    bool RequiresElevatedConsent(string scope);
    bool VerifyPkce(string codeVerifier, string codeChallenge, string codeChallengeMethod);
    bool IsAllowedRedirectUri(string redirectUri);
    bool IsValidCodeVerifier(string codeVerifier);
}
