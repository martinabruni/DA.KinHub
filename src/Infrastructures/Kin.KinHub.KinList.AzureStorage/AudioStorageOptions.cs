namespace Kin.KinHub.KinList.AzureStorage;

public sealed class AudioStorageOptions
{
    public const string SectionName = "AudioStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string BlobServiceUri { get; set; } = string.Empty;
    public string QueueServiceUri { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "kinlist-audio";
    public string ProcessingQueueName { get; set; } = "kinlist-audio-processing";
    public string PoisonQueueName { get; set; } = "kinlist-audio-poison";

    public void Validate()
    {
        var hasConnectionString = !string.IsNullOrWhiteSpace(ConnectionString);
        if (!hasConnectionString && !Uri.TryCreate(BlobServiceUri, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("AudioStorage:BlobServiceUri must be an absolute URI.");
        }

        if (!hasConnectionString && !Uri.TryCreate(QueueServiceUri, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("AudioStorage:QueueServiceUri must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(ContainerName))
        {
            throw new InvalidOperationException("AudioStorage:ContainerName is required.");
        }

        if (string.IsNullOrWhiteSpace(ProcessingQueueName))
        {
            throw new InvalidOperationException("AudioStorage:ProcessingQueueName is required.");
        }

        if (string.IsNullOrWhiteSpace(PoisonQueueName))
        {
            throw new InvalidOperationException("AudioStorage:PoisonQueueName is required.");
        }
    }
}
