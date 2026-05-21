using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.ChatFeature;

public sealed class ChatConversation : BaseEntity<Guid>
{
    public required Guid FamilyMemberId { get; set; }
    public required string Title { get; set; }
}
