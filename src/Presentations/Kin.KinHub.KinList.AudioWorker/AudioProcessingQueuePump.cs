using Azure.Storage.Queues.Models;
using Kin.KinHub.KinList.AzureStorage;

namespace Kin.KinHub.KinList.AudioWorker;

public interface IAudioProcessingQueuePump
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AudioProcessingQueueMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken);
    Task DeleteMessageAsync(AudioProcessingQueueMessage message, CancellationToken cancellationToken);
    Task SendPoisonMessageAsync(string payload, CancellationToken cancellationToken);
    Task RenewMessageVisibilityAsync(AudioProcessingQueueMessage message, TimeSpan visibilityTimeout, CancellationToken cancellationToken);
}

public sealed class AudioProcessingQueueMessage
{
    public required string MessageId { get; set; }
    public required string PopReceipt { get; set; }
    public required string MessageText { get; set; }
    public int DequeueCount { get; set; }
}

internal sealed class AzureAudioProcessingQueuePump : IAudioProcessingQueuePump
{
    private readonly AzureStorageAudioClients _clients;

    public AzureAudioProcessingQueuePump(AzureStorageAudioClients clients)
    {
        _clients = clients;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _clients.ContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await _clients.ProcessingQueueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await _clients.PoisonQueueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<AudioProcessingQueueMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
    {
        QueueMessage[] messages = (await _clients.ProcessingQueueClient.ReceiveMessagesAsync(
            maxMessages: maxMessages,
            visibilityTimeout: visibilityTimeout,
            cancellationToken: cancellationToken)).Value;

        return messages.Select(Map).ToList();
    }

    public Task DeleteMessageAsync(AudioProcessingQueueMessage message, CancellationToken cancellationToken) =>
        _clients.ProcessingQueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);

    public Task SendPoisonMessageAsync(string payload, CancellationToken cancellationToken) =>
        _clients.PoisonQueueClient.SendMessageAsync(payload, cancellationToken);

    public async Task RenewMessageVisibilityAsync(AudioProcessingQueueMessage message, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
    {
        var response = await _clients.ProcessingQueueClient.UpdateMessageAsync(
            message.MessageId,
            message.PopReceipt,
            message.MessageText,
            visibilityTimeout: visibilityTimeout,
            cancellationToken: cancellationToken);
        message.PopReceipt = response.Value.PopReceipt;
    }

    private static AudioProcessingQueueMessage Map(QueueMessage message) => new()
    {
        MessageId = message.MessageId,
        PopReceipt = message.PopReceipt,
        MessageText = message.MessageText,
        DequeueCount = checked((int)message.DequeueCount),
    };
}
