namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class CreateAudioProcessingOperationRequest
{
    public required string Type { get; set; }
    public required string ContentType { get; set; }
    public long DeclaredByteSize { get; set; }
    public Guid? ListId { get; set; }
}
