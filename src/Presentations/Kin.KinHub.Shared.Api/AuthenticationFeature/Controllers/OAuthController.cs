using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Kin.KinHub.Shared.Api.AuthenticationFeature;

[ApiController]
[Route("")]
public sealed class OAuthController : ControllerBase
{
    private static readonly string[] SupportedGrantTypes = ["authorization_code", "refresh_token"];
    private static readonly string[] SupportedResponseTypes = ["code"];

    private readonly IAuthenticationService _authenticationService;
    private readonly IOAuthClientStore _clientStore;
    private readonly IOAuthAuthorizationCodeStore _authorizationCodeStore;
    private readonly ITokenValidator _tokenValidator;
    private readonly McpTransportOptions _mcpOptions;

    public OAuthController(
        IAuthenticationService authenticationService,
        IOAuthClientStore clientStore,
        IOAuthAuthorizationCodeStore authorizationCodeStore,
        ITokenValidator tokenValidator,
        McpTransportOptions mcpOptions)
    {
        _authenticationService = authenticationService;
        _clientStore = clientStore;
        _authorizationCodeStore = authorizationCodeStore;
        _tokenValidator = tokenValidator;
        _mcpOptions = mcpOptions;
    }

    [HttpPost("register")]
    public IActionResult RegisterClient([FromBody] OAuthDynamicClientRegistrationRequest? request)
    {
        if (request is null)
        {
            return BadRequest(CreateOAuthError("invalid_client_metadata", "Missing client metadata."));
        }

        request.RedirectUris ??= [];
        request.GrantTypes ??= [];
        request.ResponseTypes ??= [];

        if (request.RedirectUris.Length is 0)
        {
            return BadRequest(CreateOAuthError("invalid_redirect_uri", "At least one redirect_uri is required."));
        }

        if (request.GrantTypes.Length is 0)
        {
            request.GrantTypes = ["authorization_code", "refresh_token"];
        }

        if (request.ResponseTypes.Length is 0)
        {
            request.ResponseTypes = ["code"];
        }

        if (!request.GrantTypes.All(SupportedGrantTypes.Contains))
        {
            return BadRequest(CreateOAuthError("invalid_client_metadata", "Only authorization_code and refresh_token grant types are supported."));
        }

        if (!request.ResponseTypes.All(SupportedResponseTypes.Contains))
        {
            return BadRequest(CreateOAuthError("invalid_client_metadata", "Only the code response type is supported."));
        }

        if (!string.IsNullOrWhiteSpace(request.TokenEndpointAuthMethod)
            && !string.Equals(request.TokenEndpointAuthMethod, "none", StringComparison.Ordinal))
        {
            return BadRequest(CreateOAuthError("invalid_client_metadata", "Only public clients with token_endpoint_auth_method 'none' are supported."));
        }

        if (request.RedirectUris.Any(uri => !IsAllowedRedirectUri(uri)))
        {
            return BadRequest(CreateOAuthError("invalid_redirect_uri", "Redirect URIs must use HTTPS or localhost."));
        }

        var client = _clientStore.Create(request, string.Join(' ', _mcpOptions.SupportedScopes));

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                client_id = client.ClientId,
                client_id_issued_at = client.ClientIdIssuedAt.ToUnixTimeSeconds(),
                client_name = client.ClientName,
                redirect_uris = client.RedirectUris,
                grant_types = client.GrantTypes,
                response_types = client.ResponseTypes,
                token_endpoint_auth_method = client.TokenEndpointAuthMethod,
                scope = client.Scope,
            });
    }

    [HttpGet("authorize")]
    public IActionResult Authorize([FromQuery] OAuthAuthorizeRequest request)
    {
        if (!TryValidateAuthorizationRequest(request, out var client, out var scope, out var errorResult))
        {
            return errorResult!;
        }

        return Content(RenderLoginPage(request, client!, scope), "text/html");
    }

    [HttpPost("authorize")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> AuthorizeAsync([FromForm] OAuthAuthorizeLoginRequest request, CancellationToken cancellationToken)
    {
        var authorizeRequest = new OAuthAuthorizeRequest
        {
            ResponseType = request.ResponseType,
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
            Scope = request.Scope,
            State = request.State,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
        };

        if (!TryValidateAuthorizationRequest(authorizeRequest, out var client, out var scope, out var errorResult))
        {
            return errorResult!;
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Content(RenderLoginPage(authorizeRequest, client!, scope, "Email and password are required."), "text/html");
        }

        var result = await _authenticationService.LoginAsync(
            new LoginRequest
            {
                Email = request.Email.Trim(),
                Password = request.Password,
            },
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return Content(
                RenderLoginPage(authorizeRequest, client!, scope, result.Message ?? "Authentication failed."),
                "text/html");
        }

        if (_tokenValidator.ValidateAccessToken(result.Value.AccessToken) is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateOAuthError("server_error", "Unable to validate issued access token."));
        }

        var ticket = _authorizationCodeStore.Create(
            client!.ClientId,
            authorizeRequest.RedirectUri!,
            scope,
            authorizeRequest.CodeChallenge!,
            authorizeRequest.CodeChallengeMethod!,
            result.Value,
            TimeSpan.FromMinutes(_mcpOptions.AuthorizationCodeLifetimeMinutes));

        var redirectParameters = new Dictionary<string, string?>
        {
            ["code"] = ticket.Code,
        };

        if (!string.IsNullOrWhiteSpace(authorizeRequest.State))
        {
            redirectParameters["state"] = authorizeRequest.State;
        }

        return Redirect(QueryHelpers.AddQueryString(authorizeRequest.RedirectUri!, redirectParameters));
    }

    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> TokenAsync([FromForm] OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId)
            || !_clientStore.TryGet(request.ClientId, out var client)
            || client is null)
        {
            return BadRequest(CreateOAuthError("invalid_client", "Unknown client_id."));
        }

        return request.GrantType switch
        {
            "authorization_code" => ExchangeAuthorizationCodeAsync(request, client),
            "refresh_token" => await ExchangeRefreshTokenAsync(request, cancellationToken),
            _ => BadRequest(CreateOAuthError("unsupported_grant_type", "The requested grant_type is not supported.")),
        };
    }

    private async Task<IActionResult> ExchangeRefreshTokenAsync(OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(CreateOAuthError("invalid_request", "refresh_token is required."));
        }

        var result = await _authenticationService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(CreateOAuthError("invalid_grant", result.Message ?? "Invalid refresh token."));
        }

        return Ok(CreateTokenResponse(result.Value, NormalizeScope(request.Scope)));
    }

    private IActionResult ExchangeAuthorizationCodeAsync(OAuthTokenRequest request, OAuthRegisteredClient client)
    {
        if (string.IsNullOrWhiteSpace(request.Code)
            || string.IsNullOrWhiteSpace(request.RedirectUri)
            || string.IsNullOrWhiteSpace(request.CodeVerifier))
        {
            return BadRequest(CreateOAuthError("invalid_request", "code, redirect_uri, and code_verifier are required."));
        }

        if (!_authorizationCodeStore.TryConsume(request.Code, out var ticket) || ticket is null)
        {
            return BadRequest(CreateOAuthError("invalid_grant", "Invalid or expired authorization code."));
        }

        if (!string.Equals(ticket.ClientId, client.ClientId, StringComparison.Ordinal)
            || !string.Equals(ticket.RedirectUri, request.RedirectUri, StringComparison.Ordinal))
        {
            return BadRequest(CreateOAuthError("invalid_grant", "Authorization code was not issued for this client or redirect_uri."));
        }

        if (!VerifyPkce(request.CodeVerifier, ticket.CodeChallenge, ticket.CodeChallengeMethod))
        {
            return BadRequest(CreateOAuthError("invalid_grant", "PKCE verification failed."));
        }

        return Ok(CreateTokenResponse(ticket.LoginResponse, ticket.Scope));
    }

    private bool TryValidateAuthorizationRequest(
        OAuthAuthorizeRequest request,
        out OAuthRegisteredClient? client,
        out string scope,
        out IActionResult? errorResult)
    {
        client = null;
        scope = NormalizeScope(request.Scope);
        errorResult = null;

        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal))
        {
            errorResult = BadRequest(CreateOAuthError("unsupported_response_type", "Only response_type=code is supported."));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ClientId)
            || !_clientStore.TryGet(request.ClientId, out client)
            || client is null)
        {
            errorResult = BadRequest(CreateOAuthError("invalid_client", "Unknown client_id."));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RedirectUri)
            || !client.RedirectUris.Contains(request.RedirectUri, StringComparer.Ordinal)
            || !IsAllowedRedirectUri(request.RedirectUri))
        {
            errorResult = BadRequest(CreateOAuthError("invalid_redirect_uri", "Invalid redirect_uri."));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.CodeChallenge)
            || !string.Equals(request.CodeChallengeMethod, "S256", StringComparison.Ordinal))
        {
            errorResult = BadRequest(CreateOAuthError("invalid_request", "PKCE with code_challenge_method=S256 is required."));
            return false;
        }

        var requestedScopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (requestedScopes.Except(_mcpOptions.SupportedScopes, StringComparer.Ordinal).Any())
        {
            errorResult = StatusCode(StatusCodes.Status403Forbidden, CreateOAuthError("invalid_scope", "Requested scope is not supported."));
            return false;
        }

        return true;
    }

    private string NormalizeScope(string? scope) =>
        string.IsNullOrWhiteSpace(scope)
            ? string.Join(' ', _mcpOptions.SupportedScopes)
            : string.Join(' ', scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static object CreateOAuthError(string error, string description) =>
        new
        {
            error,
            error_description = description,
        };

    private static bool VerifyPkce(string codeVerifier, string codeChallenge, string codeChallengeMethod)
    {
        if (!string.Equals(codeChallengeMethod, "S256", StringComparison.Ordinal))
        {
            return false;
        }

        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var computedChallenge = Base64UrlEncoder.Encode(hash);
        return string.Equals(computedChallenge, codeChallenge, StringComparison.Ordinal);
    }

    private static bool IsAllowedRedirectUri(string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private object CreateTokenResponse(LoginResponse response, string scope) =>
        new
        {
            access_token = response.AccessToken,
            token_type = "Bearer",
            expires_in = response.ExpiresIn,
            refresh_token = response.RefreshToken,
            scope,
        };

    private static string RenderLoginPage(
        OAuthAuthorizeRequest request,
        OAuthRegisteredClient client,
        string scope,
        string? errorMessage = null)
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var errorBlock = string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : $"<p style=\"color:#b91c1c;margin:0 0 16px;\">{Encode(errorMessage)}</p>";

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>KinHub OAuth</title>
</head>
<body style="font-family:system-ui,sans-serif;background:#f8fafc;color:#0f172a;padding:32px;">
    <main style="max-width:420px;margin:0 auto;background:white;padding:24px;border-radius:12px;box-shadow:0 10px 30px rgba(15,23,42,.08);">
        <h1 style="margin-top:0;">Authorize {{Encode(client.ClientName)}}</h1>
        <p style="margin:0 0 16px;">Sign in to grant access to scope <strong>{{Encode(scope)}}</strong>.</p>
        {{errorBlock}}
        <form method="post" action="/authorize">
            <input type="hidden" name="response_type" value="{{Encode(request.ResponseType)}}" />
            <input type="hidden" name="client_id" value="{{Encode(request.ClientId)}}" />
            <input type="hidden" name="redirect_uri" value="{{Encode(request.RedirectUri)}}" />
            <input type="hidden" name="scope" value="{{Encode(scope)}}" />
            <input type="hidden" name="state" value="{{Encode(request.State)}}" />
            <input type="hidden" name="code_challenge" value="{{Encode(request.CodeChallenge)}}" />
            <input type="hidden" name="code_challenge_method" value="{{Encode(request.CodeChallengeMethod)}}" />
            <label style="display:block;margin-bottom:12px;">
                <span style="display:block;margin-bottom:6px;">Email</span>
                <input type="email" name="email" autocomplete="username" required style="width:100%;padding:10px;border:1px solid #cbd5e1;border-radius:8px;" />
            </label>
            <label style="display:block;margin-bottom:16px;">
                <span style="display:block;margin-bottom:6px;">Password</span>
                <input type="password" name="password" autocomplete="current-password" required style="width:100%;padding:10px;border:1px solid #cbd5e1;border-radius:8px;" />
            </label>
            <button type="submit" style="width:100%;padding:10px 16px;border:0;border-radius:8px;background:#2563eb;color:white;font-weight:600;">Continue</button>
        </form>
    </main>
</body>
</html>
""";
    }
}
