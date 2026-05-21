namespace Kin.KinHub.Core.Domain.ChatFeature;

public sealed class ChatServiceResponse
{
    public string? TextContent { get; init; }
    public ChatToolCallRequest? ToolCallRequest { get; init; }
    public bool IsToolCall => ToolCallRequest is not null;
}
