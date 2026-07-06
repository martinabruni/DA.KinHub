namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IAudioProcessingQueue
{
    Task EnqueueAsync(Guid operationId, string correlationId, CancellationToken cancellationToken = default);
}
