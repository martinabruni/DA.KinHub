namespace Kin.KinHub.Core.Business.ChatFeature;

public sealed class ConversationWithMessages
{
    public required ChatConversation Conversation { get; init; }
    public required IReadOnlyList<ChatMessage> Messages { get; init; }
    public required IReadOnlyList<ChatToolCall> PendingToolCalls { get; init; }
}
