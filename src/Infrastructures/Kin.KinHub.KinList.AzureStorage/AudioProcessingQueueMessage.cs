namespace Kin.KinHub.KinList.AzureStorage;

public sealed class AudioProcessingQueueMessage
{
    public string MessageId { get; init; } = string.Empty;
    public string PopReceipt { get; set; } = string.Empty;
    public string MessageText { get; init; } = string.Empty;
    public int DequeueCount { get; init; }
}
