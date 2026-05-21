using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.ChatFeature;

public sealed class ChatMessageRepository
    : PostgreSqlRepository<ChatMessageEntity, ChatMessage, Guid>, IChatMessageRepository
{
    public ChatMessageRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChatMessage>> GetLastAsync(
        Guid conversationId,
        int count,
        CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.ConversationId == conversationId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<ChatMessage>>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChatMessage>> GetByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.ConversationId == conversationId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<ChatMessage>>();
    }
}
