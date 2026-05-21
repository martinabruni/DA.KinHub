namespace Kin.KinHub.Core.Business.ChatFeature;

public interface IChatToolExecutor
{
    Task<Result<ChatToolExecutionResult>> ExecuteAsync(
        ChatToolCall toolCall,
        Guid userId,
        CancellationToken cancellationToken = default);
}
