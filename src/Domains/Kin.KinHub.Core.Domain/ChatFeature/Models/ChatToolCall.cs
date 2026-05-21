using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.ChatFeature;

public sealed class ChatToolCall : BaseEntity<Guid>
{
    public required Guid MessageId { get; set; }
    public required string ToolName { get; set; }
    public required string ArgumentsJson { get; set; }
    public required ChatToolCallStatus Status { get; set; }
}
