namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IAudioProcessingQueuePublisher
{
    Task EnqueueAsync(Guid operationId, string correlationId, CancellationToken cancellationToken = default);
}
