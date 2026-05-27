using Kin.KinHub.Shared.Api.McpFeature.Contracts;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

public interface IMcpDispatcher
{
    Task<McpDispatchResult> DispatchAsync(McpJsonRpcMessage message, string? sessionId, CancellationToken cancellationToken);
}

public sealed record McpDispatchResult
{
    public McpJsonRpcResponse? Response { get; init; }
    public string? CreatedSessionId { get; init; }
}
