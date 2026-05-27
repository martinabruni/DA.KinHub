namespace Kin.KinHub.Shared.Api.Common.Mcp;

public sealed class McpTransportOptions
{
    public const string SectionName = "Mcp";
    public const string EndpointRoute = "api/v1/mcp";

    public string ProtocolVersion { get; set; } = "2025-03-26";
    public string ServerName { get; set; } = "Kin.KinHub.Shared.Api";
    public string ServerVersion { get; set; } = "1.0.0";
    public string Instructions { get; set; } =
        "Use the available KinHub tools to authenticate and manage families, recipes, shopping lists, fridges, and recipe assistant workflows.";
    public bool RequireSessionHeader { get; set; } = true;
    public bool AllowAnyOrigin { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = [];
    public int SessionIdleTimeoutMinutes { get; set; } = 30;

    public bool IsOriginAllowed(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return true;

        if (AllowAnyOrigin || AllowedOrigins.Length is 0)
            return true;

        return AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}
