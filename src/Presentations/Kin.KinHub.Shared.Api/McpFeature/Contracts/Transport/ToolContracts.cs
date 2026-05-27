using System.Text.Json;

namespace Kin.KinHub.Shared.Api.McpFeature.Contracts.Transport;

public sealed class McpToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required JsonElement InputSchema { get; init; }
}

public sealed class McpToolListResult
{
    public required IReadOnlyList<McpToolDefinition> Tools { get; init; }
    public string? NextCursor { get; init; }
}

public sealed class McpToolCallParams
{
    public required string Name { get; init; }
    public JsonElement Arguments { get; init; }
}

public sealed record McpToolCallResult
{
    public IReadOnlyList<McpToolContentItem> Content { get; init; } = [];
    public bool IsError { get; init; }
}

public sealed class McpToolContentItem
{
    public required string Type { get; init; }
    public required string Text { get; init; }
}
