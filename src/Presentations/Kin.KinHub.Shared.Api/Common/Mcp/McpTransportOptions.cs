namespace Kin.KinHub.Shared.Api.Common.Mcp;

public sealed class McpTransportOptions
{
    public const string CorsPolicyName = "KinHubMcpPolicy";
    public const string SectionName = "Mcp";
    public const string EndpointRoute = "api/v1/mcp";

    public string ProtocolVersion { get; set; } = "2025-03-26";
    public string ServerName { get; set; } = "Kin.KinHub.Shared.Api";
    public string ServerVersion { get; set; } = "1.0.0";
    public string Instructions { get; set; } =
        "Authenticate via the KinHub OAuth 2.1 flow, then use the available KinHub tools to manage families, recipes, shopping lists, fridges, and recipe assistant workflows.";
    public string ResourceName { get; set; } = "KinHub MCP";
    public string ResourceDocumentation { get; set; } = "https://github.com/martinabruni/Kin.KinHub";
    public string AuthorizationServerUrl { get; set; } = "http://localhost";
    public string[] SupportedScopes { get; set; } = ["mcp:tools"];
    public int AuthorizationCodeLifetimeMinutes { get; set; } = 5;
    public bool AllowAnyOrigin { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = [];
}
