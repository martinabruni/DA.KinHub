using System.Diagnostics;
using System.Text.Json;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.Shared.Kernel.Common;

namespace Kin.KinHub.KinList.AudioWorker;

public sealed class AudioQueueMessageProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAudioProcessingQueuePump _queuePump;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KinListOptions _kinListOptions;
    private readonly ILogger<AudioQueueMessageProcessor> _logger;
    private readonly TimeSpan _visibilityTimeout;
    private readonly TimeSpan _visibilityRenewInterval;

    public AudioQueueMessageProcessor(
        IAudioProcessingQueuePump queuePump,
        IServiceScopeFactory scopeFactory,
        KinListOptions kinListOptions,
        ILogger<AudioQueueMessageProcessor> logger)
    {
        _queuePump = queuePump;
        _scopeFactory = scopeFactory;
        _kinListOptions = kinListOptions;
        _logger = logger;
        _visibilityTimeout = TimeSpan.FromSeconds(Math.Max(_kinListOptions.AudioProcessingTimeoutSeconds, 30));
        _visibilityRenewInterval = TimeSpan.FromSeconds(Math.Max(15, _visibilityTimeout.TotalSeconds / 2));
    }

    public async Task<AudioQueueMessageDisposition> ProcessAsync(
        string messageText,
        int dequeueCount,
        string messageId,
        Func<CancellationToken, Task>? renewVisibilityAsync,
        CancellationToken cancellationToken)
    {
        AudioQueueMessage? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AudioQueueMessage>(messageText, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid audio queue message payload.");
            await _queuePump.SendPoisonMessageAsync(messageText, cancellationToken);
            return AudioQueueMessageDisposition.Delete;
        }

        if (payload is null)
        {
            await _queuePump.SendPoisonMessageAsync(messageText, cancellationToken);
            return AudioQueueMessageDisposition.Delete;
        }

        if (payload.ContractVersion != 1)
        {
            _logger.LogWarning("Unsupported audio queue contract version {ContractVersion}.", payload.ContractVersion);
            await _queuePump.SendPoisonMessageAsync(messageText, cancellationToken);
            return AudioQueueMessageDisposition.Delete;
        }

        var operationId = payload.OperationId;
        using var activity = StartMessageActivity(payload, messageId, dequeueCount);
        activity?.SetTag("kinlist.audio.operation.id", operationId);
        activity?.SetTag("kinlist.audio.correlation_id", payload.CorrelationId);
        activity?.SetTag("messaging.system", "azurestoragequeue");

        if (dequeueCount > _kinListOptions.AudioProcessingMaxDequeues)
        {
            _logger.LogWarning("Audio operation {OperationId} exceeded dequeue limit {MaxDequeues}.", operationId, _kinListOptions.AudioProcessingMaxDequeues);
            await MarkFailedAndMoveToPoisonAsync(
                operationId,
                messageText,
                "audio_processing_poisoned",
                "Audio processing exceeded the maximum retry count.",
                cancellationToken);
            return AudioQueueMessageDisposition.Delete;
        }

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAudioOperationProcessor>();
        using var renewalCts = renewVisibilityAsync is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renewalTask = renewVisibilityAsync is null
            ? null
            : RenewVisibilityAsync(renewVisibilityAsync, renewalCts!.Token);
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
                return AudioQueueMessageDisposition.Retry;
            }
        }
        finally
        {
            if (renewalCts is not null)
            {
                renewalCts.Cancel();
            }

            if (renewalTask is not null)
            {
                try
                {
                    await renewalTask;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || renewalCts?.IsCancellationRequested == true)
                {
                }
            }
        }

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Audio operation {OperationId} returned status {Status} code {Code}. Message will be retried.", operationId, result.Status, result.Code);
            return AudioQueueMessageDisposition.Retry;
        }

        if (result.Value is null)
        {
            _logger.LogWarning("Audio operation {OperationId} returned no payload. Message will be retried.", operationId);
            return AudioQueueMessageDisposition.Retry;
        }

        if (string.Equals(result.Value.Status, "Succeeded", StringComparison.OrdinalIgnoreCase)
            || string.Equals(result.Value.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return AudioQueueMessageDisposition.Delete;
        }

        _logger.LogInformation("Audio operation {OperationId} completed queue turn with state {State}.", operationId, result.Value.Status);
        return AudioQueueMessageDisposition.Retry;
    }

    private static Activity? StartMessageActivity(AudioQueueMessage payload, string messageId, int dequeueCount)
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
                    { "messaging.message.id", messageId },
                    { "messaging.message.retry.count", dequeueCount },
                });
        }

        var activity = KinListAudioTelemetry.ActivitySource.StartActivity("kinlist.audio.queue.process", ActivityKind.Consumer);
        activity?.SetTag("messaging.destination.name", "kinlist-audio-processing");
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.message.id", messageId);
        activity?.SetTag("messaging.message.retry.count", dequeueCount);
        return activity;
    }

    private async Task MarkFailedAndMoveToPoisonAsync(
        Guid operationId,
        string payload,
        string code,
        string message,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAudioOperationProcessor>();
        await service.MarkAudioOperationFailedAsync(operationId, code, message, cancellationToken);
        await _queuePump.SendPoisonMessageAsync(payload, cancellationToken);
    }

    private async Task RenewVisibilityAsync(
        Func<CancellationToken, Task> renewVisibilityAsync,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_visibilityRenewInterval, cancellationToken);
            await renewVisibilityAsync(cancellationToken);
        }
    }
}
