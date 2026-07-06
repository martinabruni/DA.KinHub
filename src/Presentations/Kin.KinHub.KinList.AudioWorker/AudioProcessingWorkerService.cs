using System.Text.Json;
using System.Diagnostics;
using Kin.KinHub.KinList.AzureStorage;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.KinList.AudioWorker;

public sealed class AudioProcessingWorkerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IAudioProcessingQueuePump _queuePump;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KinListOptions _kinListOptions;
    private readonly ILogger<AudioProcessingWorkerService> _logger;
    private readonly TimeSpan _visibilityTimeout;
    private readonly TimeSpan _visibilityRenewInterval;

    public AudioProcessingWorkerService(
        IAudioProcessingQueuePump queuePump,
        IServiceScopeFactory scopeFactory,
        KinListOptions kinListOptions,
        ILogger<AudioProcessingWorkerService> logger)
    {
        _queuePump = queuePump;
        _scopeFactory = scopeFactory;
        _kinListOptions = kinListOptions;
        _logger = logger;
        _visibilityTimeout = TimeSpan.FromSeconds(Math.Max(_kinListOptions.AudioProcessingTimeoutSeconds, 30));
        _visibilityRenewInterval = TimeSpan.FromSeconds(Math.Max(15, _visibilityTimeout.TotalSeconds / 2));
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
        AudioQueueMessage? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AudioQueueMessage>(message.MessageText, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid audio queue message payload.");
            await MoveToPoisonAsync(message, message.MessageText, cancellationToken);
            return;
        }

        if (payload is null)
        {
            await MoveToPoisonAsync(message, message.MessageText, cancellationToken);
            return;
        }

        if (payload.ContractVersion != 1)
        {
            _logger.LogWarning("Unsupported audio queue contract version {ContractVersion}.", payload.ContractVersion);
            await MoveToPoisonAsync(message, message.MessageText, cancellationToken);
            return;
        }

        var operationId = payload.OperationId;
        using var activity = StartMessageActivity(payload, message);
        activity?.SetTag("kinlist.audio.operation.id", operationId);
        activity?.SetTag("kinlist.audio.correlation_id", payload.CorrelationId);
        activity?.SetTag("messaging.system", "azurestoragequeue");

        if (message.DequeueCount > _kinListOptions.AudioProcessingMaxDequeues)
        {
            _logger.LogWarning("Audio operation {OperationId} exceeded dequeue limit {MaxDequeues}.", operationId, _kinListOptions.AudioProcessingMaxDequeues);
            await MarkFailedAndMoveToPoisonAsync(operationId, message, message.MessageText, "audio_processing_poisoned", "Audio processing exceeded the maximum retry count.", cancellationToken);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAudioOperationProcessor>();
        using var renewalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renewalTask = RenewVisibilityAsync(message, renewalCts.Token);
        Result<AudioProcessingOperationResponse> result;
        try
        {
            try
            {
                result = await service.ProcessAudioOperationAsync(operationId, cancellationToken);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Audio operation {OperationId} failed unexpectedly. Message will be retried.", operationId);
                return;
            }
        }
        finally
        {
            renewalCts.Cancel();
            try
            {
                await renewalTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || renewalCts.IsCancellationRequested)
            {
            }
        }

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Audio operation {OperationId} returned status {Status} code {Code}. Message will be retried.", operationId, result.Status, result.Code);
            return;
        }

        if (result.Value is null)
        {
            _logger.LogWarning("Audio operation {OperationId} returned no payload. Message will be retried.", operationId);
            return;
        }

        if (string.Equals(result.Value.Status, "Succeeded", StringComparison.OrdinalIgnoreCase)
            || string.Equals(result.Value.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            await _queuePump.DeleteMessageAsync(message, cancellationToken);
            return;
        }

        _logger.LogInformation("Audio operation {OperationId} completed queue turn with state {State}.", operationId, result.Value.Status);
    }

    private static Activity? StartMessageActivity(AudioQueueMessage payload, AudioProcessingQueueMessage message)
    {
        if (KinListAudioTelemetry.TryParseCorrelationContext(payload.CorrelationId, out var parentContext))
        {
            return KinListAudioTelemetry.ActivitySource.StartActivity(
                "kinlist.audio.queue.process",
                ActivityKind.Consumer,
                parentContext,
                tags: new ActivityTagsCollection
                {
                    { "messaging.destination.name", "kinlist-audio-processing" },
                    { "messaging.operation", "process" },
                    { "messaging.message.id", message.MessageId },
                    { "messaging.message.retry.count", message.DequeueCount },
                });
        }

        var activity = KinListAudioTelemetry.ActivitySource.StartActivity("kinlist.audio.queue.process", ActivityKind.Consumer);
        activity?.SetTag("messaging.destination.name", "kinlist-audio-processing");
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.message.id", message.MessageId);
        activity?.SetTag("messaging.message.retry.count", message.DequeueCount);
        return activity;
    }

    private async Task MoveToPoisonAsync(AudioProcessingQueueMessage message, string payload, CancellationToken cancellationToken)
    {
        await _queuePump.SendPoisonMessageAsync(payload, cancellationToken);
        await _queuePump.DeleteMessageAsync(message, cancellationToken);
    }

    private async Task MarkFailedAndMoveToPoisonAsync(Guid operationId, AudioProcessingQueueMessage message, string payload, string code, string messageText, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAudioOperationProcessor>();
        await service.MarkAudioOperationFailedAsync(operationId, code, messageText, cancellationToken);
        await MoveToPoisonAsync(message, payload, cancellationToken);
    }

    private async Task RenewVisibilityAsync(AudioProcessingQueueMessage message, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_visibilityRenewInterval, cancellationToken);
            await _queuePump.RenewMessageVisibilityAsync(message, _visibilityTimeout, cancellationToken);
        }
    }
}
