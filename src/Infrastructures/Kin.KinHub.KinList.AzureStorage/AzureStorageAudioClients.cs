using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

namespace Kin.KinHub.KinList.AzureStorage;

public sealed class AzureStorageAudioClients
{
    public AzureStorageAudioClients(AudioStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            BlobServiceClient = new BlobServiceClient(options.ConnectionString);
            QueueServiceClient = new QueueServiceClient(options.ConnectionString, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64,
            });
            SharedKeyCredential = CreateSharedKeyCredential(options.ConnectionString);
        }
        else
        {
            var credential = new DefaultAzureCredential();
            BlobServiceClient = new BlobServiceClient(new Uri(options.BlobServiceUri), credential);
            QueueServiceClient = new QueueServiceClient(new Uri(options.QueueServiceUri), credential, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64,
            });
        }

        ContainerClient = BlobServiceClient.GetBlobContainerClient(options.ContainerName);
        ProcessingQueueClient = QueueServiceClient.GetQueueClient(options.ProcessingQueueName);
        PoisonQueueClient = QueueServiceClient.GetQueueClient(options.PoisonQueueName);
    }

    public BlobServiceClient BlobServiceClient { get; }
    public QueueServiceClient QueueServiceClient { get; }
    public StorageSharedKeyCredential? SharedKeyCredential { get; }
    public BlobContainerClient ContainerClient { get; }
    public QueueClient ProcessingQueueClient { get; }
    public QueueClient PoisonQueueClient { get; }

    private static StorageSharedKeyCredential CreateSharedKeyCredential(string connectionString)
    {
        var values = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

        if (!values.TryGetValue("AccountName", out var accountName) || string.IsNullOrWhiteSpace(accountName))
        {
            throw new InvalidOperationException("AudioStorage:ConnectionString must contain AccountName.");
        }

        if (!values.TryGetValue("AccountKey", out var accountKey) || string.IsNullOrWhiteSpace(accountKey))
        {
            throw new InvalidOperationException("AudioStorage:ConnectionString must contain AccountKey.");
        }

        return new StorageSharedKeyCredential(accountName, accountKey);
    }
}
