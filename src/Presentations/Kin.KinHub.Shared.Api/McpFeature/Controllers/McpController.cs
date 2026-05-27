using Kin.KinHub.Shared.Api.McpFeature.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Kin.KinHub.Shared.Api.McpFeature.Controllers;

[ApiController]
[Route(McpTransportOptions.EndpointRoute)]
public sealed class McpController : ControllerBase
{
    private readonly McpRequestValidator _requestValidator;
    private readonly IMcpDispatcher _dispatcher;
    private readonly IMcpSessionService _sessionService;

    public McpController(
        McpRequestValidator requestValidator,
        IMcpDispatcher dispatcher,
        IMcpSessionService sessionService)
    {
        _requestValidator = requestValidator;
        _dispatcher = dispatcher;
        _sessionService = sessionService;
    }

    [HttpGet]
    public IActionResult Get() =>
        StatusCode(StatusCodes.Status405MethodNotAllowed);

    [HttpDelete]
    public IActionResult Delete()
    {
        if (!_requestValidator.TryValidateTransport(Request, out var transportError))
            return transportError!;

        var sessionId = Request.Headers["Mcp-Session-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { message = "Mcp-Session-Id header is required." });

        if (!_sessionService.TerminateSession(sessionId))
            return NotFound();

        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync(CancellationToken cancellationToken)
    {
        if (!_requestValidator.TryValidateTransport(Request, out var transportError))
            return transportError!;

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return CreateJsonResult(
                McpErrorMapper.JsonRpcError(null, McpErrorMapper.ParseError, "Invalid JSON payload."),
                StatusCodes.Status400BadRequest);
        }

        using var _ = document;

        var isBatch = document.RootElement.ValueKind is JsonValueKind.Array;
        var messages = DeserializeMessages(document.RootElement, isBatch);
        var sessionId = Request.Headers["Mcp-Session-Id"].FirstOrDefault();

        if (!_requestValidator.TryValidateMessages(messages, isBatch, sessionId, _sessionService, out var statusCode, out var validationError))
        {
            return validationError is null
                ? StatusCode(statusCode)
                : CreateJsonResult(validationError, statusCode);
        }

        var responses = new List<McpJsonRpcResponse>();
        string? createdSessionId = null;

        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Method))
                continue;

            var result = await _dispatcher.DispatchAsync(message, sessionId, cancellationToken);
            createdSessionId ??= result.CreatedSessionId;

            if (result.Response is not null)
            {
                responses.Add(result.Response);
            }
        }

        if (!string.IsNullOrWhiteSpace(createdSessionId))
        {
            Response.Headers.Append("Mcp-Session-Id", createdSessionId);
        }

        if (responses.Count is 0)
            return Accepted();

        return isBatch
            ? CreateJsonResult(responses, StatusCodes.Status200OK)
            : CreateJsonResult(responses[0], StatusCodes.Status200OK);
    }

    private static IReadOnlyList<McpJsonRpcMessage> DeserializeMessages(JsonElement root, bool isBatch)
    {
        if (isBatch)
        {
            return root.Deserialize(McpJsonSerializerContext.Default.McpJsonRpcMessageArray)
                ?? [];
        }

        var message = root.Deserialize(McpJsonSerializerContext.Default.McpJsonRpcMessage);
        return message is null ? [] : [message];
    }

    private static JsonResult CreateJsonResult(object payload, int statusCode) =>
        new(payload, McpJsonSerializerContext.Default.Options)
        {
            StatusCode = statusCode,
            ContentType = "application/json",
        };

}
