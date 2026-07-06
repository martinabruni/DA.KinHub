using Azure.Storage.Sas;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.KinList.AzureStorage;

internal sealed class AzureBlobAudioProcessingBlobStorage : IAudioProcessingBlobStorage
{
    private readonly AzureStorageAudioClients _clients;

    public AzureBlobAudioProcessingBlobStorage(AzureStorageAudioClients clients)
    {
        _clients = clients;
    }

    public async Task<AudioBlobUploadTarget> CreateUploadTargetAsync(string blobName, string contentType, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        var blobClient = _clients.ContainerClient.GetBlobClient(blobName);
        var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
        var expiresOn = startsOn.Add(timeToLive);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _clients.ContainerClient.Name,
            BlobName = blobName,
            Resource = "b",
            StartsOn = startsOn,
            ExpiresOn = expiresOn,
            ContentType = contentType,
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        string sasQuery;
        if (_clients.SharedKeyCredential is { } sharedKeyCredential)
        {
            sasQuery = sasBuilder.ToSasQueryParameters(sharedKeyCredential).ToString();
        }
        else
        {
            var delegationKey = await _clients.BlobServiceClient.GetUserDelegationKeyAsync(startsOn, expiresOn, cancellationToken);
            sasQuery = sasBuilder.ToSasQueryParameters(delegationKey.Value, _clients.BlobServiceClient.AccountName).ToString();
        }
        var uriBuilder = new UriBuilder(blobClient.Uri)
        {
            Query = sasQuery,
        };

        return new AudioBlobUploadTarget
        {
            BlobName = blobName,
            UploadUrl = uriBuilder.Uri,
            ExpiresAt = expiresOn.UtcDateTime,
        };
    }

    public async Task<AudioBlobDescriptor?> GetBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _clients.ContainerClient.GetBlobClient(blobName);
        var exists = await blobClient.ExistsAsync(cancellationToken);
        if (!exists.Value)
        {
            return null;
        }

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        return new AudioBlobDescriptor
        {
            BlobName = blobName,
            ContentType = properties.Value.ContentType,
            ContentLength = properties.Value.ContentLength,
        };
    }

    public async Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _clients.ContainerClient.GetBlobClient(blobName);
        return await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _clients.ContainerClient.GetBlobClient(blobName);
        return blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
