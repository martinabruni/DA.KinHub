using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.ChatFeature;

public sealed class ChatMessage : BaseEntity<Guid>
{
    public required Guid ConversationId { get; set; }
    public required ChatMessageRole Role { get; set; }
    public required string Content { get; set; }
}
