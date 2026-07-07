using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

[ApiController]
[Route("")]
public sealed class OAuthController : ControllerBase
{
    private static readonly string[] SupportedGrantTypes = ["authorization_code"];
    private static readonly string[] SupportedResponseTypes = ["code"];

    private readonly IIdentitySessionService _sessionService;
    private readonly IOAuthClientStore _clientStore;
    private readonly IOAuthAuthorizationCodeStore _authorizationCodeStore;
    private readonly IOAuthIdentitySessionStore _identitySessionStore;
    private readonly ITokenValidator _tokenValidator;
    private readonly IOAuthLoginPageRenderer _loginPageRenderer;
    private readonly IOAuthRequestValidator _requestValidator;
    private readonly IRequestValidator<OAuthDynamicClientRegistrationRequest> _dynamicClientRegistrationValidator;
    private readonly IOAuthSessionManager _sessionManager;
    private readonly IOAuthTokenIssuer _tokenIssuer;
    private readonly OAuthServerOptions _oauthOptions;

    public OAuthController(
        IIdentitySessionService sessionService,
        IOAuthClientStore clientStore,
        IOAuthAuthorizationCodeStore authorizationCodeStore,
        IOAuthIdentitySessionStore identitySessionStore,
        ITokenValidator tokenValidator,
        IOAuthLoginPageRenderer loginPageRenderer,
        IOAuthRequestValidator requestValidator,
        IRequestValidator<OAuthDynamicClientRegistrationRequest> dynamicClientRegistrationValidator,
        IOAuthSessionManager sessionManager,
        IOAuthTokenIssuer tokenIssuer,
        OAuthServerOptions oauthOptions)
    {
        _sessionService = sessionService;
        _clientStore = clientStore;
        _authorizationCodeStore = authorizationCodeStore;
        _identitySessionStore = identitySessionStore;
        _tokenValidator = tokenValidator;
        _loginPageRenderer = loginPageRenderer;
        _requestValidator = requestValidator;
        _dynamicClientRegistrationValidator = dynamicClientRegistrationValidator;
        _sessionManager = sessionManager;
        _tokenIssuer = tokenIssuer;
        _oauthOptions = oauthOptions;
    }

    [HttpPost("register")]
    [EnableRateLimiting(OAuthServerOptions.RateLimitPolicyName)]
    public async Task<IActionResult> RegisterClient([FromBody] OAuthDynamicClientRegistrationRequest? request)
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

        if (request.GrantTypes.Length is 0)
        {
            request.GrantTypes = ["authorization_code"];
        }

        if (request.ResponseTypes.Length is 0)
        {
            request.ResponseTypes = ["code"];
        }

        var validation = await _dynamicClientRegistrationValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(CreateOAuthError("invalid_client_metadata", validation.Errors[0]));
        }

        if (!_requestValidator.TryResolveDynamicClientScope(this, request.Scope, out var resolvedScope, out var scopeErrorResult))
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
    public async Task<IActionResult> AuthorizeAsync([FromQuery] OAuthAuthorizeRequest request, CancellationToken cancellationToken)
    {
        if (!_requestValidator.TryValidateAuthorizationRequest(this, request, out var client, out var scope, out var errorResult))
        {
            return errorResult!;
        }

        if (_sessionManager.TryGetIdentitySession(Request, Response, out var session))
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
                    await _sessionManager.RehydrateLoginResponseAsync(Request, Response, session, cancellationToken),
                    TimeSpan.FromMinutes(_oauthOptions.AuthorizationCodeLifetimeMinutes));

                return Redirect(_requestValidator.BuildAuthorizationSuccessRedirect(request.RedirectUri!, request.State, ticket.Code));
            }
            catch (UnauthorizedAccessException)
            {
                _sessionManager.DeleteIdentitySessionCookie(Request, Response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, CreateOAuthError("slow_down", ex.Message));
            }
        }

        return Content(_loginPageRenderer.Render(request, client!, scope, _oauthOptions.AuthorizationServerUrl, _oauthOptions.RegistrationUiUrl), "text/html");
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

        if (!_requestValidator.TryValidateAuthorizationRequest(this, authorizeRequest, out var client, out var scope, out var errorResult))
        {
            return errorResult!;
        }

        if (string.Equals(request.Decision, "deny", StringComparison.Ordinal))
        {
            return _requestValidator.RedirectOAuthError(authorizeRequest.RedirectUri!, authorizeRequest.State, "access_denied", "The authorization request was denied.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Content(_loginPageRenderer.Render(authorizeRequest, client!, scope, _oauthOptions.AuthorizationServerUrl, _oauthOptions.RegistrationUiUrl, "Email and password are required."), "text/html");
        }

        var result = await _sessionService.LoginAsync(
            new LoginRequest
            {
                Email = request.Email.Trim(),
                Password = request.Password,
            },
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return Content(
                _loginPageRenderer.Render(authorizeRequest, client!, scope, _oauthOptions.AuthorizationServerUrl, _oauthOptions.RegistrationUiUrl, result.Message ?? "Authentication failed."),
                "text/html");
        }

        if (_requestValidator.RequiresElevatedConsent(scope) && !request.ApproveElevatedAccess)
        {
            return Content(
                _loginPageRenderer.Render(authorizeRequest, client!, scope, _oauthOptions.AuthorizationServerUrl, _oauthOptions.RegistrationUiUrl, "You must explicitly approve elevated write access before continuing."),
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

        _sessionManager.WriteIdentitySessionCookie(Request, Response, session);

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

        return Redirect(_requestValidator.BuildAuthorizationSuccessRedirect(authorizeRequest.RedirectUri!, authorizeRequest.State, ticket.Code));
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
    public async Task<IActionResult> LogoutAsync(
        [FromQuery(Name = "client_id")] string? clientId,
        [FromQuery(Name = "post_logout_redirect_uri")] string? postLogoutRedirectUri,
        CancellationToken cancellationToken)
    {
        if (_sessionManager.TryGetIdentitySessionId(Request, out var sessionId)
            && _identitySessionStore.TryRemove(sessionId, out var session)
            && session is not null)
        {
            await _sessionService.LogoutAsync(session.RefreshToken, cancellationToken);
        }

        _sessionManager.DeleteIdentitySessionCookie(Request, Response);
        if (!string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(postLogoutRedirectUri)
            && _clientStore.TryGet(clientId, out var client)
            && client is not null
            && client.RedirectUris.Contains(postLogoutRedirectUri, StringComparer.Ordinal))
        {
            return Redirect(postLogoutRedirectUri);
        }

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

        if (!_requestValidator.IsValidCodeVerifier(request.CodeVerifier))
        {
            return BadRequest(CreateOAuthError("invalid_request", "code_verifier must be 43-128 unreserved characters (RFC 7636 §4.1)."));
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

        if (!_requestValidator.VerifyPkce(request.CodeVerifier, ticket.CodeChallenge, ticket.CodeChallengeMethod))
        {
            return BadRequest(CreateOAuthError("invalid_grant", "PKCE verification failed."));
        }

        return Ok(_tokenIssuer.CreateScopedTokenResponse(ticket.LoginResponse, ticket.Scope));
    }

    private static object CreateOAuthError(string error, string description) =>
        new
        {
            error,
            error_description = description,
        };
}
