namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class AudioBlobUploadTarget
{
    public required Uri UploadUrl { get; set; }
    public required string BlobName { get; set; }
    public required DateTime ExpiresAt { get; set; }
}
