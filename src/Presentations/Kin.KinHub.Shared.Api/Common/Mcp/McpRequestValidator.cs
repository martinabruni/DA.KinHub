using Kin.KinHub.Shared.Api.McpFeature.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

public sealed class McpRequestValidator
{
    private readonly McpTransportOptions _options;

    public McpRequestValidator(McpTransportOptions options)
    {
        _options = options;
    }

    public bool TryValidateTransport(HttpRequest request, out IActionResult? errorResult)
    {
        var acceptsJson = request.Headers.Accept.Count is 0
            || request.Headers.Accept.Select(static header => header is null ? string.Empty : header.ToString()).Any(static header =>
                header.Contains("application/json", StringComparison.OrdinalIgnoreCase)
                || header.Contains("*/*", StringComparison.OrdinalIgnoreCase)
                || header.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase));

        if (!acceptsJson)
        {
            errorResult = new BadRequestObjectResult(new { message = "Accept header must allow application/json." });
            return false;
        }

        if (!_options.IsOriginAllowed(request.Headers.Origin.FirstOrDefault()))
        {
            errorResult = new BadRequestObjectResult(new { message = "Origin header is not allowed for this MCP endpoint." });
            return false;
        }

        errorResult = null;
        return true;
    }

    public bool TryValidateMessages(
        IReadOnlyList<McpJsonRpcMessage> messages,
        bool isBatch,
        string? sessionId,
        IMcpSessionService sessionService,
        out int statusCode,
        out McpJsonRpcErrorResponse? errorResponse)
    {
        statusCode = StatusCodes.Status400BadRequest;
        errorResponse = null;

        if (messages.Count is 0)
        {
            errorResponse = McpErrorMapper.JsonRpcError(null, McpErrorMapper.InvalidRequest, "The request batch cannot be empty.");
            return false;
        }

        foreach (var message in messages)
        {
            if (!string.Equals(message.JsonRpc, "2.0", StringComparison.Ordinal))
            {
                errorResponse = McpErrorMapper.JsonRpcError(message.Id, McpErrorMapper.InvalidRequest, "Only JSON-RPC 2.0 messages are supported.");
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            if (isBatch || messages.Count is not 1 || !string.Equals(messages[0].Method, "initialize", StringComparison.Ordinal))
            {
                errorResponse = McpErrorMapper.JsonRpcError(messages[0].Id, McpErrorMapper.InvalidRequest, "The first MCP request must be a non-batched initialize request.");
                return false;
            }

            if (!messages[0].HasId)
            {
                errorResponse = McpErrorMapper.JsonRpcError(null, McpErrorMapper.InvalidRequest, "The initialize request must include an id.");
                return false;
            }

            return true;
        }

        if (!sessionService.TryGetSession(sessionId, out var session))
        {
            statusCode = StatusCodes.Status404NotFound;
            errorResponse = McpErrorMapper.JsonRpcError(messages[0].Id, McpErrorMapper.InvalidRequest, "The MCP session does not exist or has expired.");
            return false;
        }

        if (messages.Any(static message => string.Equals(message.Method, "initialize", StringComparison.Ordinal)))
        {
            errorResponse = McpErrorMapper.JsonRpcError(messages[0].Id, McpErrorMapper.InvalidRequest, "Initialize can only be called when creating a new session.");
            return false;
        }

        if (!session!.ClientInitialized)
        {
            var invalidMessage = messages.FirstOrDefault(static message =>
                !string.Equals(message.Method, "notifications/initialized", StringComparison.Ordinal)
                && !string.Equals(message.Method, "ping", StringComparison.Ordinal));

            if (invalidMessage is not null)
            {
                errorResponse = McpErrorMapper.JsonRpcError(invalidMessage.Id, McpErrorMapper.InvalidRequest, "The client must send notifications/initialized before calling MCP operations.");
                return false;
            }
        }

        return true;
    }
}
