using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.ChatFeature;

public sealed class ChatToolCallRepository
    : PostgreSqlRepository<ChatToolCallEntity, ChatToolCall, Guid>, IChatToolCallRepository
{
    public ChatToolCallRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChatToolCall>> GetByMessageIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.MessageId == messageId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<ChatToolCall>>();
    }
}
