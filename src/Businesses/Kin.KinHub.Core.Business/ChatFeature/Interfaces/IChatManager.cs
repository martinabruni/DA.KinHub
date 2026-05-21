namespace Kin.KinHub.Core.Business.ChatFeature;

public interface IChatManager
{
    /// <summary>Creates a new conversation for the given family member.</summary>
    Task<Result<ChatConversation>> CreateConversationAsync(
        Guid familyMemberId,
        string title,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all conversations for the given family member, ordered newest first.</summary>
    Task<Result<IReadOnlyList<ChatConversation>>> GetConversationsAsync(
        Guid familyMemberId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a conversation with its full message history and pending tool calls.</summary>
    Task<Result<ConversationWithMessages>> GetConversationAsync(
        Guid conversationId,
        Guid familyMemberId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a conversation and all its messages.</summary>
    Task<Result<ChatConversation>> DeleteConversationAsync(
        Guid conversationId,
        Guid familyMemberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a user message to the AI and persists the response.
    /// Returns the assistant message and optionally a pending tool call awaiting confirmation.
    /// </summary>
    Task<Result<SendMessageResponse>> SendMessageAsync(
        Guid conversationId,
        Guid familyMemberId,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>Confirms a pending tool call, marking it as Confirmed.</summary>
    Task<Result<ChatToolCall>> ConfirmToolCallAsync(
        Guid toolCallId,
        Guid familyMemberId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Rejects a pending tool call, marking it as Rejected.</summary>
    Task<Result<ChatToolCall>> RejectToolCallAsync(
        Guid toolCallId,
        Guid familyMemberId,
        CancellationToken cancellationToken = default);
}
