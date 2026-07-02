using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.AuthenticationFeature;

[ApiController]
[Route(".well-known")]
public sealed class OAuthMetadataController : ControllerBase
{
    private readonly OAuthServerOptions _oauthOptions;

    public OAuthMetadataController(OAuthServerOptions oauthOptions)
    {
        _oauthOptions = oauthOptions;
    }

    [HttpGet("oauth-authorization-server")]
    [HttpGet("openid-configuration")]
    public IActionResult GetAuthorizationServerMetadata()
    {
        var issuer = GetIssuer();

        return Ok(new
        {
            issuer,
            authorization_endpoint = $"{issuer}/authorize",
            token_endpoint = $"{issuer}/token",
            registration_endpoint = _oauthOptions.EnableDynamicClientRegistration ? $"{issuer}/register" : null,
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code" },
            token_endpoint_auth_methods_supported = new[] { "none" },
            code_challenge_methods_supported = new[] { "S256" },
            response_modes_supported = new[] { "query" },
            scopes_supported = _oauthOptions.SupportedScopes,
            service_documentation = _oauthOptions.DocumentationUrl,
        });
    }

    private string GetIssuer()
    {
        if (!string.IsNullOrWhiteSpace(_oauthOptions.AuthorizationServerUrl))
        {
            return _oauthOptions.AuthorizationServerUrl.TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host}";
    }
}
