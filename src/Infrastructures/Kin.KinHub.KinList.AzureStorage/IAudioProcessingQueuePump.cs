namespace Kin.KinHub.KinList.AzureStorage;

public interface IAudioProcessingQueuePump
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AudioProcessingQueueMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken);
    Task DeleteMessageAsync(AudioProcessingQueueMessage message, CancellationToken cancellationToken);
    Task SendPoisonMessageAsync(string payload, CancellationToken cancellationToken);
    Task RenewMessageVisibilityAsync(AudioProcessingQueueMessage message, TimeSpan visibilityTimeout, CancellationToken cancellationToken);
}
