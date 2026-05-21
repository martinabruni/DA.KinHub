using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.ChatFeature;

public interface IChatToolCallRepository : IRepository<ChatToolCall, Guid>
{
    /// <summary>
    /// Returns all tool calls associated with the specified message.
    /// </summary>
    Task<IReadOnlyList<ChatToolCall>> GetByMessageIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);
}
