namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class AudioBlobDescriptor
{
    public required string BlobName { get; set; }
    public required string ContentType { get; set; }
    public long ContentLength { get; set; }
}
