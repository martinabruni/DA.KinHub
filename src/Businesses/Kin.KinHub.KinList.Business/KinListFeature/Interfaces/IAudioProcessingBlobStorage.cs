namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IAudioProcessingBlobStorage
{
    Task<AudioBlobUploadTarget> CreateUploadTargetAsync(string blobName, string contentType, TimeSpan timeToLive, CancellationToken cancellationToken = default);
    Task<AudioBlobDescriptor?> GetBlobAsync(string blobName, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken = default);
    Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default);
}
