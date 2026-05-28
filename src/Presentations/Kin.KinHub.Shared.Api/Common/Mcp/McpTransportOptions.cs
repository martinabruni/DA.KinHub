namespace Kin.KinHub.Shared.Api.Common.Mcp;

public sealed class McpTransportOptions
{
    public const string CorsPolicyName = "KinHubMcpPolicy";
    public const string OAuthRateLimitPolicyName = "KinHubOAuthPolicy";
    public const string SectionName = "Mcp";
    public const string EndpointRoute = "api/v1/mcp";

    public string ProtocolVersion { get; set; } = "2025-03-26";
    public string ServerName { get; set; } = "Kin.KinHub.Shared.Api";
    public string ServerVersion { get; set; } = "1.0.0";
    public string Instructions { get; set; } =
        "Authenticate via the KinHub OAuth 2.1 flow, then use the available KinHub tools to manage families, recipes, shopping lists, fridges, and recipe assistant workflows.";
    public string ResourceName { get; set; } = "KinHub MCP";
    public string ResourceDocumentation { get; set; } = "https://github.com/martinabruni/Kin.KinHub";
    public string AuthorizationServerUrl { get; set; } = "https://localhost";
    public string[] SupportedScopes { get; set; } = [McpScopes.Read, McpScopes.Write, McpScopes.Admin];
    public string[] DynamicClientDefaultScopes { get; set; } = [McpScopes.Read];
    public string[] DynamicClientAllowedScopes { get; set; } = [McpScopes.Read, McpScopes.Write];
    public string[] ElevatedConsentScopes { get; set; } = [McpScopes.Write, McpScopes.Admin];
    public int AuthorizationCodeLifetimeMinutes { get; set; } = 5;
    public bool EnableDynamicClientRegistration { get; set; }
    public int MaxRegisteredClients { get; set; } = 100;
    public int MaxAuthorizationCodes { get; set; } = 1000;
    public int MaxScopedRefreshTokens { get; set; } = 1000;
    public int OAuthRateLimitPermitLimit { get; set; } = 30;
    public int OAuthRateLimitWindowSeconds { get; set; } = 60;
    public bool AllowAnyOrigin { get; set; } = false;
    public string[] AllowedOrigins { get; set; } = [];
}
