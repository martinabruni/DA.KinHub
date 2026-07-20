using DA.KinHub.Domain.Documents;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Infrastructure.Storage;

internal sealed class BlobDocumentStorage : IDocumentStorage
{
    private readonly BlobContainerClient container;

    public BlobDocumentStorage(IOptions<BlobStorageOptions> options, TokenCredential credential)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            throw new InvalidOperationException("Storage:ContainerName is required.");
        }

        container = !string.IsNullOrWhiteSpace(settings.ConnectionString)
            ? new BlobContainerClient(settings.ConnectionString, settings.ContainerName)
            : new BlobContainerClient(BuildContainerUri(settings), credential);
    }

    public async Task<StoredDocument> SaveAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(content);

        var extension = Path.GetExtension(Path.GetFileName(fileName));
        var now = DateTimeOffset.UtcNow;
        var key = $"{now:yyyy/MM}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var blob = container.GetBlobClient(key);

        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        return new StoredDocument(key, blob.Uri, contentType, properties.Value.ContentLength);
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var response = await container.GetBlobClient(key).DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await container.DeleteBlobIfExistsAsync(key, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }

    private static Uri BuildContainerUri(BlobStorageOptions settings)
    {
        if (!Uri.TryCreate(settings.AccountUri, UriKind.Absolute, out var accountUri))
        {
            throw new InvalidOperationException("Storage:AccountUri must be an absolute URI when no connection string is configured.");
        }

        return new Uri(accountUri, settings.ContainerName);
    }
}
