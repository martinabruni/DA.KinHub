using System.Text.Json;
using CoreResult = Kin.KinHub.Core.Business.Common;
using IdentityResult = Kin.KinHub.Identity.Business.Common;
using Kin.KinHub.Shared.Api.McpFeature.Contracts;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

internal static class McpErrorMapper
{
    internal const int ParseError = -32700;
    internal const int InvalidRequest = -32600;
    internal const int MethodNotFound = -32601;
    internal const int InvalidParams = -32602;
    internal const int InternalError = -32603;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static McpJsonRpcErrorResponse JsonRpcError(JsonElement? id, int code, string message, object? data = null) =>
        new()
        {
            Id = id,
            Error = new McpJsonRpcError
            {
                Code = code,
                Message = message,
                Data = data is null ? null : JsonSerializer.SerializeToElement(data, SerializerOptions),
            },
        };

    public static McpToolCallResult ToolSuccess(object? value)
    {
        var json = value is null ? "{}" : JsonSerializer.Serialize(value, SerializerOptions);
        return new McpToolCallResult
        {
            IsError = false,
            Content =
            [
                new McpToolContentItem
                {
                    Type = "text",
                    Text = json,
                },
            ],
        };
    }

    public static McpToolCallResult ToolError(string message, string? code = null)
    {
        object payload = code is null
            ? new { message }
            : new { code, message };

        return new McpToolCallResult
        {
            IsError = true,
            Content =
            [
                new McpToolContentItem
                {
                    Type = "text",
                    Text = JsonSerializer.Serialize(payload, SerializerOptions),
                },
            ],
        };
    }

    public static McpToolCallResult ToolError(IReadOnlyList<string> errors) =>
        ToolError("Validation failed.", "validation_error_with_details") with
        {
            Content =
            [
                new McpToolContentItem
                {
                    Type = "text",
                    Text = JsonSerializer.Serialize(new { message = "Validation failed.", errors }, SerializerOptions),
                },
            ],
        };

    public static McpToolCallResult FromCoreResult<T>(CoreResult.Result<T> result) =>
        result.IsSuccess
            ? ToolSuccess(result.Value)
            : ToolError(result.Message ?? "Unexpected business error.", result.Status.ToString());

    public static McpToolCallResult FromIdentityResult<T>(IdentityResult.Result<T> result) =>
        result.IsSuccess
            ? ToolSuccess(result.Value)
            : ToolError(result.Message ?? "Unexpected business error.", result.Status.ToString());
}
