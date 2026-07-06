using Kin.KinHub.KinList.Business.Common;

namespace Kin.KinHub.KinList.Business.KinListFeature;

internal sealed class UnavailableAudioProcessingBlobStorage : IAudioProcessingBlobStorage
{
    public Task<AudioBlobUploadTarget> CreateUploadTargetAsync(string blobName, string contentType, TimeSpan timeToLive, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Audio blob storage is not configured.");

    public Task<AudioBlobDescriptor?> GetBlobAsync(string blobName, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Audio blob storage is not configured.");

    public Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Audio blob storage is not configured.");

    public Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Audio blob storage is not configured.");
}
