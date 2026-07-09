namespace Kin.KinHub.KinList.Business.KinListFeature;

internal sealed class UnavailableAudioProcessingQueuePublisher : IAudioProcessingQueuePublisher
{
    public Task EnqueueAsync(Guid operationId, string correlationId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Audio processing queue is not configured.");
}
