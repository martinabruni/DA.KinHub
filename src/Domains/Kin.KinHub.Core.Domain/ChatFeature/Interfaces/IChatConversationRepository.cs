using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.ChatFeature;

public interface IChatConversationRepository : IRepository<ChatConversation, Guid>
{
    /// <summary>
    /// Returns all conversations belonging to the specified family member.
    /// </summary>
    Task<IReadOnlyList<ChatConversation>> GetByFamilyMemberIdAsync(
        Guid familyMemberId,
        CancellationToken cancellationToken = default);
}
