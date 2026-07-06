namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class AudioBlobUploadTarget
{
    public required Uri UploadUrl { get; set; }
    public required string BlobName { get; set; }
    public required DateTime ExpiresAt { get; set; }
}

public sealed class AudioBlobDescriptor
{
    public required string BlobName { get; set; }
    public required string ContentType { get; set; }
    public long ContentLength { get; set; }
}
