using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kin.KinHub.Shared.Api.McpFeature.Contracts;

public sealed class McpJsonRpcMessage
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public JsonElement? Id { get; init; }

    [JsonPropertyName("method")]
    public string? Method { get; init; }

    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }

    [JsonPropertyName("result")]
    public JsonElement? Result { get; init; }

    [JsonPropertyName("error")]
    public McpJsonRpcError? Error { get; init; }

    [JsonIgnore]
    public bool HasId => Id.HasValue
        && Id.Value.ValueKind is not JsonValueKind.Null
        && Id.Value.ValueKind is not JsonValueKind.Undefined;
}

public sealed class McpJsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }
}

public abstract class McpJsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; init; }
}

public sealed class McpJsonRpcSuccessResponse : McpJsonRpcResponse
{
    [JsonPropertyName("result")]
    public object? Result { get; init; }
}

public sealed class McpJsonRpcErrorResponse : McpJsonRpcResponse
{
    [JsonPropertyName("error")]
    public required McpJsonRpcError Error { get; init; }
}
