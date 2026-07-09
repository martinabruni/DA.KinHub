namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IAudioProcessingQueueConsumer
{
    Task<AudioQueueMessageDisposition> ProcessAsync(
        string messageText,
        int dequeueCount,
        string messageId,
        Func<CancellationToken, Task>? renewVisibilityAsync,
        CancellationToken cancellationToken);
}
