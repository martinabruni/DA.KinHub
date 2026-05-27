using Kin.KinHub.Shared.Api.McpFeature.Contracts;
using System.Text.Json.Serialization;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(McpJsonRpcMessage))]
[JsonSerializable(typeof(McpJsonRpcMessage[]))]
[JsonSerializable(typeof(McpJsonRpcSuccessResponse))]
[JsonSerializable(typeof(McpJsonRpcSuccessResponse[]))]
[JsonSerializable(typeof(McpJsonRpcErrorResponse))]
[JsonSerializable(typeof(McpJsonRpcErrorResponse[]))]
[JsonSerializable(typeof(McpInitializeRequestParams))]
[JsonSerializable(typeof(McpInitializeResult))]
[JsonSerializable(typeof(McpToolListResult))]
[JsonSerializable(typeof(McpToolDefinition))]
[JsonSerializable(typeof(List<McpToolDefinition>))]
[JsonSerializable(typeof(McpToolCallParams))]
[JsonSerializable(typeof(McpToolCallResult))]
[JsonSerializable(typeof(McpToolContentItem))]
internal sealed partial class McpJsonSerializerContext : JsonSerializerContext
{
}
