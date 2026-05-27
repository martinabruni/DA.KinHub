using System.Text.Json;

namespace Kin.KinHub.Shared.Api.McpFeature.Contracts.Transport;

public sealed class McpInitializeRequestParams
{
    public required string ProtocolVersion { get; init; }
    public JsonElement? Capabilities { get; init; }
    public required McpClientInfo ClientInfo { get; init; }
}

public sealed class McpClientInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}

public sealed class McpInitializeResult
{
    public required string ProtocolVersion { get; init; }
    public required McpServerCapabilities Capabilities { get; init; }
    public required McpServerInfo ServerInfo { get; init; }
    public required string Instructions { get; init; }
}

public sealed class McpServerCapabilities
{
    public required McpToolsCapability Tools { get; init; }
}

public sealed class McpToolsCapability
{
    public bool ListChanged { get; init; }
}

public sealed class McpServerInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}
