namespace Kin.KinHub.Core.Business.ChatFeature;

public sealed class SendMessageResponse
{
    public required ChatMessage AssistantMessage { get; init; }
    public ChatToolCall? PendingToolCall { get; init; }
    public bool HasPendingToolCall => PendingToolCall is not null;
}
