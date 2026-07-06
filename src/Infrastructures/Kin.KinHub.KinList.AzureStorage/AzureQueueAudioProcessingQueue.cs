using System.Text.Json;
using System.Diagnostics;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.KinList.AzureStorage;

internal sealed class AzureQueueAudioProcessingQueue : IAudioProcessingQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AzureStorageAudioClients _clients;

    public AzureQueueAudioProcessingQueue(AzureStorageAudioClients clients)
    {
        _clients = clients;
    }

    public Task EnqueueAsync(Guid operationId, string correlationId, CancellationToken cancellationToken = default)
    {
        using var activity = KinListAudioTelemetry.ActivitySource.StartActivity("kinlist.audio.queue.enqueue", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "azurestoragequeue");
        activity?.SetTag("messaging.destination.name", _clients.ProcessingQueueClient.Name);
        activity?.SetTag("kinlist.audio.operation.id", operationId);

        var payload = JsonSerializer.Serialize(new AudioQueueMessage
        {
            OperationId = operationId,
            CorrelationId = KinListAudioTelemetry.ResolveCorrelationId(correlationId),
        }, JsonOptions);

        return _clients.ProcessingQueueClient.SendMessageAsync(payload, cancellationToken);
    }
}
