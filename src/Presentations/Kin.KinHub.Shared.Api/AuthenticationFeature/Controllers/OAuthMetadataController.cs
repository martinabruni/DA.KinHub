using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.AuthenticationFeature;

[ApiController]
[Route(".well-known")]
public sealed class OAuthMetadataController : ControllerBase
{
    private readonly McpTransportOptions _mcpOptions;

    public OAuthMetadataController(McpTransportOptions mcpOptions)
    {
        _mcpOptions = mcpOptions;
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
            registration_endpoint = $"{issuer}/register",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            token_endpoint_auth_methods_supported = new[] { "none" },
            code_challenge_methods_supported = new[] { "S256" },
            response_modes_supported = new[] { "query" },
            scopes_supported = _mcpOptions.SupportedScopes,
            service_documentation = _mcpOptions.ResourceDocumentation,
        });
    }

    private string GetIssuer()
    {
        if (!string.IsNullOrWhiteSpace(_mcpOptions.AuthorizationServerUrl))
        {
            return _mcpOptions.AuthorizationServerUrl.TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host}";
    }
}
