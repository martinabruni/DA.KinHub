namespace Kin.KinHub.App.Functions.KinListFeature;

public sealed class AudioQueueFunctions
{
    private readonly IAudioProcessingQueueConsumer _processor;

    public AudioQueueFunctions(IAudioProcessingQueueConsumer processor)
    {
        _processor = processor;
    }

    [Function(nameof(ProcessAsync))]
    public async Task ProcessAsync(
        [QueueTrigger("%AudioStorage:ProcessingQueueName%", Connection = "AzureWebJobsStorage")] string messageText,
        int dequeueCount,
        string id,
        CancellationToken cancellationToken)
    {
        var disposition = await _processor.ProcessAsync(messageText, dequeueCount, id, renewVisibilityAsync: null, cancellationToken);
        if (disposition is AudioQueueMessageDisposition.Retry)
        {
            throw new InvalidOperationException("Audio queue message should be retried.");
        }
    }
}
