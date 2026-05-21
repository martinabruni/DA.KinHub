namespace Kin.KinHub.Core.Domain.ChatFeature;

public sealed class ChatToolCallRequest
{
    public required string ToolName { get; init; }
    public required string ArgumentsJson { get; init; }
}
