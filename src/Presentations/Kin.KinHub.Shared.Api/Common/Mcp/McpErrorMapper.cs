using ModelContextProtocol.Protocol;
using System.Text.Json;
using CoreResult = Kin.KinHub.Core.Business.Common;
using IdentityResult = Kin.KinHub.Identity.Business.Common;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

internal static class McpErrorMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static CallToolResult ToolSuccess(object? value)
    {
        var json = value is null ? "{}" : JsonSerializer.Serialize(value, SerializerOptions);
        return new CallToolResult
        {
            IsError = false,
            StructuredContent = value is null ? null : JsonSerializer.SerializeToElement(value, SerializerOptions),
            Content =
            [
                new TextContentBlock
                {
                    Text = json,
                },
            ],
        };
    }

    public static CallToolResult ToolError(string message, string? code = null)
    {
        object payload = code is null
            ? new { message }
            : new { code, message };

        return new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(payload, SerializerOptions),
                },
            ],
        };
    }

    public static CallToolResult ToolError(IReadOnlyList<string> errors) =>
        new()
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(new { message = "Validation failed.", errors }, SerializerOptions),
                },
            ],
        };

    public static CallToolResult FromCoreResult<T>(CoreResult.Result<T> result) =>
        result.IsSuccess
            ? ToolSuccess(result.Value)
            : ToolError(result.Message ?? "Unexpected business error.", result.Status.ToString());

    public static CallToolResult FromIdentityResult<T>(IdentityResult.Result<T> result) =>
        result.IsSuccess
            ? ToolSuccess(result.Value)
            : ToolError(result.Message ?? "Unexpected business error.", result.Status.ToString());
}
