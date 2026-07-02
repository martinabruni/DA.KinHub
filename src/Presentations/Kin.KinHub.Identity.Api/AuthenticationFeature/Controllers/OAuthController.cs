using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

[ApiController]
[Route("")]
public sealed class OAuthController : ControllerBase
{
    private static readonly string[] SupportedGrantTypes = ["authorization_code"];
    private static readonly string[] SupportedResponseTypes = ["code"];
    private const string SessionCookiePath = "/";

    private readonly IAuthenticationService _authenticationService;
    private readonly IOAuthClientStore _clientStore;
    private readonly IOAuthAuthorizationCodeStore _authorizationCodeStore;
    private readonly IOAuthIdentitySessionStore _identitySessionStore;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenValidator _tokenValidator;
    private readonly OAuthServerOptions _oauthOptions;

    public OAuthController(
        IAuthenticationService authenticationService,
        IOAuthClientStore clientStore,
        IOAuthAuthorizationCodeStore authorizationCodeStore,
        IOAuthIdentitySessionStore identitySessionStore,
        ITokenGenerator tokenGenerator,
        ITokenValidator tokenValidator,
        OAuthServerOptions oauthOptions)
    {
        _authenticationService = authenticationService;
        _clientStore = clientStore;
        _authorizationCodeStore = authorizationCodeStore;
        _identitySessionStore = identitySessionStore;
        _tokenGenerator = tokenGenerator;
        _tokenValidator = tokenValidator;
        _oauthOptions = oauthOptions;
    }

    [HttpPost("register")]
    [EnableRateLimiting(OAuthServerOptions.RateLimitPolicyName)]
    public IActionResult RegisterClient([FromBody] OAuthDynamicClientRegistrationRequest? request)
    {
        if (!_oauthOptions.EnableDynamicClientRegistration)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateOAuthError("access_denied", "Dynamic client registration is disabled."));
        }

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
            request.GrantTypes = ["authorization_code"];
        }

        if (request.ResponseTypes.Length is 0)
        {
            request.ResponseTypes = ["code"];
        }

        if (!request.GrantTypes.All(SupportedGrantTypes.Contains))
        {
            return BadRequest(CreateOAuthError("invalid_client_metadata", "Only the authorization_code grant type is supported."));
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

        if (!TryResolveDynamicClientScope(request.Scope, out var resolvedScope, out var scopeErrorResult))
        {
            return scopeErrorResult!;
        }

        request.Scope = resolvedScope;

        OAuthRegisteredClient client;
        try
        {
            client = _clientStore.Create(request, resolvedScope);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, CreateOAuthError("slow_down", ex.Message));
        }

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
    [EnableRateLimiting(OAuthServerOptions.RateLimitPolicyName)]
    public IActionResult Authorize([FromQuery] OAuthAuthorizeRequest request)
    {
        if (!TryValidateAuthorizationRequest(request, out var client, out var scope, out var errorResult))
        {
            return errorResult!;
        }

        if (TryGetIdentitySession(out var session))
        {
            try
            {
                ArgumentNullException.ThrowIfNull(session);
                var ticket = _authorizationCodeStore.Create(
                    client!.ClientId,
                    request.RedirectUri!,
                    scope,
                    request.CodeChallenge!,
                    request.CodeChallengeMethod!,
                    RehydrateLoginResponse(session),
                    TimeSpan.FromMinutes(_oauthOptions.AuthorizationCodeLifetimeMinutes));

                return Redirect(BuildAuthorizationSuccessRedirect(request.RedirectUri!, request.State, ticket.Code));
            }
            catch (UnauthorizedAccessException)
            {
                DeleteIdentitySessionCookie();
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, CreateOAuthError("slow_down", ex.Message));
            }
        }

        return Content(RenderLoginPage(request, client!, scope, _oauthOptions.RegistrationUiUrl), "text/html");
    }

    [HttpPost("authorize")]
    [Consumes("application/x-www-form-urlencoded")]
    [EnableRateLimiting(OAuthServerOptions.RateLimitPolicyName)]
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

        if (string.Equals(request.Decision, "deny", StringComparison.Ordinal))
        {
            return RedirectOAuthError(authorizeRequest.RedirectUri!, authorizeRequest.State, "access_denied", "The authorization request was denied.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Content(RenderLoginPage(authorizeRequest, client!, scope, _oauthOptions.RegistrationUiUrl, "Email and password are required."), "text/html");
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

        if (RequiresElevatedConsent(scope) && !request.ApproveElevatedAccess)
        {
            return Content(
                RenderLoginPage(authorizeRequest, client!, scope, _oauthOptions.RegistrationUiUrl, "You must explicitly approve elevated write access before continuing."),
                "text/html");
        }

        if (_tokenValidator.ValidateAccessToken(result.Value.AccessToken) is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateOAuthError("server_error", "Unable to validate issued access token."));
        }

        OAuthIdentitySession session;
        try
        {
            session = _identitySessionStore.Create(
                result.Value,
                TimeSpan.FromHours(_oauthOptions.SessionLifetimeHours));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, CreateOAuthError("slow_down", ex.Message));
        }

        WriteIdentitySessionCookie(session);

        OAuthAuthorizationCodeTicket ticket;
        try
        {
            ticket = _authorizationCodeStore.Create(
                client!.ClientId,
                authorizeRequest.RedirectUri!,
                scope,
                authorizeRequest.CodeChallenge!,
                authorizeRequest.CodeChallengeMethod!,
                result.Value,
                TimeSpan.FromMinutes(_oauthOptions.AuthorizationCodeLifetimeMinutes));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, CreateOAuthError("slow_down", ex.Message));
        }

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
    [EnableRateLimiting(OAuthServerOptions.RateLimitPolicyName)]
    public IActionResult Token([FromForm] OAuthTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId)
            || !_clientStore.TryGet(request.ClientId, out var client)
            || client is null)
        {
            return BadRequest(CreateOAuthError("invalid_client", "Unknown client_id."));
        }

        return request.GrantType switch
        {
            "authorization_code" => ExchangeAuthorizationCode(request, client),
            _ => BadRequest(CreateOAuthError("unsupported_grant_type", "The requested grant_type is not supported.")),
        };
    }

    [HttpPost("logout")]
    [EnableRateLimiting(OAuthServerOptions.RateLimitPolicyName)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        if (TryGetIdentitySessionId(out var sessionId)
            && _identitySessionStore.TryRemove(sessionId, out var session)
            && session is not null)
        {
            await _authenticationService.LogoutAsync(session.RefreshToken, cancellationToken);
        }

        DeleteIdentitySessionCookie();
        return NoContent();
    }

    private IActionResult ExchangeAuthorizationCode(OAuthTokenRequest request, OAuthRegisteredClient client)
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

        return Ok(CreateScopedTokenResponse(ticket.LoginResponse, ticket.Scope));
    }

    private bool TryValidateAuthorizationRequest(
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

        if (!TryNormalizeGrantedScope(request.Scope, client.Scope, client.Scope, out scope, out errorResult))
        {
            return false;
        }

        return true;
    }

    private bool TryNormalizeGrantedScope(
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
            errorResult = StatusCode(StatusCodes.Status403Forbidden, CreateOAuthError("invalid_scope", "Requested scope is not supported for this client grant."));
            return false;
        }

        return true;
    }

    private bool TryResolveDynamicClientScope(
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
            errorResult = BadRequest(CreateOAuthError("invalid_scope", "At least one scope is required."));
            return false;
        }

        if (scopes.Except(supportedScopes, StringComparer.Ordinal).Any())
        {
            errorResult = StatusCode(StatusCodes.Status403Forbidden, CreateOAuthError("invalid_scope", "Requested scope is not supported by KinHub."));
            return false;
        }

        if (scopes.Except(dynamicClientAllowedScopes, StringComparer.Ordinal).Any())
        {
            errorResult = StatusCode(StatusCodes.Status403Forbidden, CreateOAuthError("invalid_scope", "Dynamic clients cannot request the specified scope."));
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

    private bool RequiresElevatedConsent(string scope)
    {
        var elevatedScopes = _oauthOptions.ElevatedConsentScopes.ToHashSet(StringComparer.Ordinal);
        return SplitScopes(scope).Intersect(elevatedScopes, StringComparer.Ordinal).Any();
    }

    private static IActionResult RedirectOAuthError(string redirectUri, string? state, string error, string description)
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

    private object CreateScopedTokenResponse(LoginResponse response, string scope)
    {
        var claims = _tokenValidator.ValidateAccessToken(response.AccessToken);
        if (claims is null)
        {
            throw new InvalidOperationException("Unable to validate issued access token.");
        }

        var user = new KinUser
        {
            Id = claims.UserId,
            Email = claims.Email,
            DisplayName = response.DisplayName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var accessToken = _tokenGenerator.GenerateAccessToken(
            user,
            claims.Roles,
            scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        // No refresh_token is issued: the refresh_token grant has been removed. SPAs renew
        // access tokens by performing a silent top-level authorize against the identity
        // session cookie.
        return new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = response.ExpiresIn,
            scope,
        };
    }

    private bool TryGetIdentitySession(out OAuthIdentitySession? session)
    {
        session = null;
        if (!TryGetIdentitySessionId(out var sessionId))
        {
            return false;
        }

        if (!_identitySessionStore.TryGet(sessionId, out var storedSession) || storedSession is null)
        {
            DeleteIdentitySessionCookie();
            return false;
        }

        session = storedSession;
        return session is not null;
    }

    private static string BuildAuthorizationSuccessRedirect(string redirectUri, string? state, string code)
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

    private LoginResponse RehydrateLoginResponse(OAuthIdentitySession session)
    {
        var refreshResult = _authenticationService.RefreshTokenAsync(session.RefreshToken, HttpContext.RequestAborted)
            .GetAwaiter()
            .GetResult();

        if (!refreshResult.IsSuccess || refreshResult.Value is null)
        {
            _identitySessionStore.TryRemove(session.SessionId, out _);
            throw new UnauthorizedAccessException("Stored identity session is no longer valid.");
        }

        _identitySessionStore.Replace(
            session.SessionId,
            refreshResult.Value,
            TimeSpan.FromHours(_oauthOptions.SessionLifetimeHours));
        WriteIdentitySessionCookie(new OAuthIdentitySession(
            session.SessionId,
            refreshResult.Value.RefreshToken,
            refreshResult.Value.Email,
            refreshResult.Value.DisplayName,
            DateTimeOffset.UtcNow.AddHours(_oauthOptions.SessionLifetimeHours)));

        return refreshResult.Value;
    }

    private bool TryGetIdentitySessionId(out string sessionId)
    {
        sessionId = Request.Cookies[_oauthOptions.SessionCookieName] ?? string.Empty;
        return !string.IsNullOrWhiteSpace(sessionId);
    }

    private void WriteIdentitySessionCookie(OAuthIdentitySession session)
    {
        Response.Cookies.Append(
            _oauthOptions.SessionCookieName,
            session.SessionId,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Path = SessionCookiePath,
                Expires = session.ExpiresAtUtc,
            });
    }

    private void DeleteIdentitySessionCookie()
    {
        Response.Cookies.Delete(
            _oauthOptions.SessionCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Path = SessionCookiePath,
            });
    }

    private static string RenderLoginPage(
        OAuthAuthorizeRequest request,
        OAuthRegisteredClient client,
        string scope,
        string registrationUiUrl,
        string? errorMessage = null)
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var errorBlock = string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : $"<p style=\"color:#b91c1c;margin:0 0 16px;\">{Encode(errorMessage)}</p>";
        var scopes = SplitScopes(scope);
        var hasElevatedScope = scopes.Contains(OAuthScopes.Write, StringComparer.Ordinal) || scopes.Contains(OAuthScopes.Admin, StringComparer.Ordinal);
        var scopeList = string.Join(
            string.Empty,
            scopes.Select(scopeValue => $"<li style=\"display:inline-block;margin:0 8px 8px 0;padding:6px 10px;border-radius:999px;background:#e2e8f0;font-size:14px;\">{Encode(scopeValue)}</li>"));
        var elevatedConsentBlock = hasElevatedScope
            ? """
        <label style="display:flex;gap:10px;align-items:flex-start;margin:0 0 16px;padding:12px;border:1px solid #fecaca;border-radius:10px;background:#fff1f2;">
            <input type="checkbox" name="approve_elevated_access" value="true" style="margin-top:4px;" />
            <span>I understand this client is requesting elevated access that can modify or delete KinHub data.</span>
        </label>
"""
            : string.Empty;
        var registerBlock = string.IsNullOrWhiteSpace(registrationUiUrl)
            ? string.Empty
            : $$"""
        <p style="margin:16px 0 0;font-size:14px;color:#475569;">
            Need an account?
            <a href="{{Encode(QueryHelpers.AddQueryString(registrationUiUrl, "returnTo", QueryHelpers.AddQueryString(request.RedirectUri!, new Dictionary<string, string?>
            {
                ["response_type"] = request.ResponseType,
                ["client_id"] = request.ClientId,
                ["redirect_uri"] = request.RedirectUri,
                ["scope"] = scope,
                ["state"] = request.State,
                ["code_challenge"] = request.CodeChallenge,
                ["code_challenge_method"] = request.CodeChallengeMethod,
            })))}}" style="color:#2563eb;text-decoration:none;font-weight:600;">Create one here</a>.
        </p>
""";

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
        <p style="margin:0 0 8px;">Sign in to review the requested KinHub scopes for <strong>{{Encode(client.ClientName)}}</strong>.</p>
        <ul style="list-style:none;padding:0;margin:0 0 16px;">{{scopeList}}</ul>
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
            {{elevatedConsentBlock}}
            <div style="display:flex;gap:12px;">
                <button type="submit" name="decision" value="approve" style="flex:1;padding:10px 16px;border:0;border-radius:8px;background:#2563eb;color:white;font-weight:600;">Continue</button>
                <button type="submit" name="decision" value="deny" formnovalidate style="flex:1;padding:10px 16px;border:1px solid #cbd5e1;border-radius:8px;background:white;color:#0f172a;font-weight:600;">Deny</button>
            </div>
        </form>
        {{registerBlock}}
    </main>
</body>
</html>
""";
    }
}
