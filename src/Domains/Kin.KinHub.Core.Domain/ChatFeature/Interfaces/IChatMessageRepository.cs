using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.ChatFeature;

public interface IChatMessageRepository : IRepository<ChatMessage, Guid>
{
    /// <summary>
    /// Returns the last <paramref name="count"/> messages for the conversation, ordered by CreatedAt ascending.
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetLastAsync(
        Guid conversationId,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all messages belonging to the specified conversation.
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}
