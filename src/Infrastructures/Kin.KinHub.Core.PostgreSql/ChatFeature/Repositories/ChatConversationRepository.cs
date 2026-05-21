using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.ChatFeature;

public sealed class ChatConversationRepository
    : PostgreSqlRepository<ChatConversationEntity, ChatConversation, Guid>, IChatConversationRepository
{
    public ChatConversationRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChatConversation>> GetByFamilyMemberIdAsync(
        Guid familyMemberId,
        CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.FamilyMemberId == familyMemberId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<ChatConversation>>();
    }
}
