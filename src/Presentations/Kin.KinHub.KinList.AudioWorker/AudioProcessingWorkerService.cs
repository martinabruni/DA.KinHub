using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.KinList.AudioWorker;

public sealed class AudioProcessingWorkerService : BackgroundService
{
    private readonly IAudioProcessingQueuePump _queuePump;
    private readonly AudioQueueMessageProcessor _processor;
    private readonly ILogger<AudioProcessingWorkerService> _logger;
    private readonly TimeSpan _visibilityTimeout;

    public AudioProcessingWorkerService(
        IAudioProcessingQueuePump queuePump,
        AudioQueueMessageProcessor processor,
        KinListOptions kinListOptions,
        ILogger<AudioProcessingWorkerService> logger)
    {
        _queuePump = queuePump;
        _processor = processor;
        _logger = logger;
        _visibilityTimeout = TimeSpan.FromSeconds(Math.Max(kinListOptions.AudioProcessingTimeoutSeconds, 30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _queuePump.InitializeAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _queuePump.ReceiveMessagesAsync(1, _visibilityTimeout, stoppingToken);

            if (messages.Count is 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            foreach (var message in messages)
            {
                await ProcessMessageAsync(message, stoppingToken);
            }
        }
    }

    public async Task ProcessMessageAsync(AudioProcessingQueueMessage message, CancellationToken cancellationToken)
    {
        var disposition = await _processor.ProcessAsync(
            message.MessageText,
            message.DequeueCount,
            message.MessageId,
            token => _queuePump.RenewMessageVisibilityAsync(message, _visibilityTimeout, token),
            cancellationToken);

        if (disposition is AudioQueueMessageDisposition.Delete)
        {
            await _queuePump.DeleteMessageAsync(message, cancellationToken);
        }
    }
}
