using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

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
}

public sealed class OAuthRequestValidator : IOAuthRequestValidator
{
    private readonly IOAuthClientStore _clientStore;
    private readonly OAuthServerOptions _oauthOptions;

    public OAuthRequestValidator(IOAuthClientStore clientStore, OAuthServerOptions oauthOptions)
    {
        _clientStore = clientStore;
        _oauthOptions = oauthOptions;
    }

    public bool TryValidateAuthorizationRequest(
        ControllerBase controller,
        OAuthAuthorizeRequest request,
        out OAuthRegisteredClient? client,
        out string scope,
        out IActionResult? errorResult)
    {
        client = null;
        scope = string.Empty;
        errorResult = null;

        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal))
        {
            errorResult = controller.BadRequest(CreateOAuthError("unsupported_response_type", "Only response_type=code is supported."));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ClientId)
            || !_clientStore.TryGet(request.ClientId, out client)
            || client is null)
        {
            errorResult = controller.BadRequest(CreateOAuthError("invalid_client", "Unknown client_id."));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RedirectUri)
            || !client.RedirectUris.Contains(request.RedirectUri, StringComparer.Ordinal)
            || !IsAllowedRedirectUri(request.RedirectUri))
        {
            errorResult = controller.BadRequest(CreateOAuthError("invalid_redirect_uri", "Invalid redirect_uri."));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.CodeChallenge)
            || !string.Equals(request.CodeChallengeMethod, "S256", StringComparison.Ordinal))
        {
            errorResult = controller.BadRequest(CreateOAuthError("invalid_request", "PKCE with code_challenge_method=S256 is required."));
            return false;
        }

        if (!TryNormalizeGrantedScope(controller, request.Scope, client.Scope, client.Scope, out scope, out errorResult))
        {
            return false;
        }

        return true;
    }

    public bool TryResolveDynamicClientScope(
        ControllerBase controller,
        string? requestedScope,
        out string resolvedScope,
        out IActionResult? errorResult)
    {
        errorResult = null;
        resolvedScope = NormalizeScope(requestedScope, string.Join(' ', _oauthOptions.DynamicClientDefaultScopes));

        var supportedScopes = _oauthOptions.SupportedScopes.ToHashSet(StringComparer.Ordinal);
        var dynamicClientAllowedScopes = _oauthOptions.DynamicClientAllowedScopes.ToHashSet(StringComparer.Ordinal);
        var scopes = SplitScopes(resolvedScope);

        if (scopes.Length is 0)
        {
            errorResult = controller.BadRequest(CreateOAuthError("invalid_scope", "At least one scope is required."));
            return false;
        }

        if (scopes.Except(supportedScopes, StringComparer.Ordinal).Any())
        {
            errorResult = controller.StatusCode(StatusCodes.Status403Forbidden, CreateOAuthError("invalid_scope", "Requested scope is not supported by KinHub."));
            return false;
        }

        if (scopes.Except(dynamicClientAllowedScopes, StringComparer.Ordinal).Any())
        {
            errorResult = controller.StatusCode(StatusCodes.Status403Forbidden, CreateOAuthError("invalid_scope", "Dynamic clients cannot request the specified scope."));
            return false;
        }

        return true;
    }

    public IActionResult RedirectOAuthError(string redirectUri, string? state, string error, string description)
    {
        var redirectParameters = new Dictionary<string, string?>
        {
            ["error"] = error,
            ["error_description"] = description,
        };

        if (!string.IsNullOrWhiteSpace(state))
        {
            redirectParameters["state"] = state;
        }

        return new RedirectResult(QueryHelpers.AddQueryString(redirectUri, redirectParameters));
    }

    public string BuildAuthorizationSuccessRedirect(string redirectUri, string? state, string code)
    {
        var redirectParameters = new Dictionary<string, string?>
        {
            ["code"] = code,
        };

        if (!string.IsNullOrWhiteSpace(state))
        {
            redirectParameters["state"] = state;
        }

        return QueryHelpers.AddQueryString(redirectUri, redirectParameters);
    }

    public bool RequiresElevatedConsent(string scope)
    {
        var elevatedScopes = _oauthOptions.ElevatedConsentScopes.ToHashSet(StringComparer.Ordinal);
        return SplitScopes(scope).Intersect(elevatedScopes, StringComparer.Ordinal).Any();
    }

    public bool VerifyPkce(string codeVerifier, string codeChallenge, string codeChallengeMethod)
    {
        if (!string.Equals(codeChallengeMethod, "S256", StringComparison.Ordinal))
        {
            return false;
        }

        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var computedChallenge = Base64UrlEncoder.Encode(hash);
        return string.Equals(computedChallenge, codeChallenge, StringComparison.Ordinal);
    }

    public bool IsAllowedRedirectUri(string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryNormalizeGrantedScope(
        ControllerBase controller,
        string? requestedScope,
        string clientScope,
        string grantedScope,
        out string scope,
        out IActionResult? errorResult)
    {
        errorResult = null;
        scope = string.IsNullOrWhiteSpace(requestedScope)
            ? NormalizeScope(grantedScope)
            : NormalizeScope(requestedScope);

        var requestedScopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var supportedScopes = _oauthOptions.SupportedScopes.ToHashSet(StringComparer.Ordinal);
        var registeredClientScopes = clientScope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
        var grantedScopes = grantedScope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);

        if (requestedScopes.Except(supportedScopes, StringComparer.Ordinal).Any()
            || requestedScopes.Except(registeredClientScopes, StringComparer.Ordinal).Any()
            || requestedScopes.Except(grantedScopes, StringComparer.Ordinal).Any())
        {
            errorResult = controller.StatusCode(StatusCodes.Status403Forbidden, CreateOAuthError("invalid_scope", "Requested scope is not supported for this client grant."));
            return false;
        }

        return true;
    }

    private string NormalizeScope(string? scope, string? defaultScope = null) =>
        string.IsNullOrWhiteSpace(scope)
            ? NormalizeScope(defaultScope ?? string.Join(' ', _oauthOptions.SupportedScopes))
            : string.Join(' ', scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal));

    private static string[] SplitScopes(string? scope) =>
        string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static object CreateOAuthError(string error, string description) =>
        new
        {
            error,
            error_description = description,
        };
}
