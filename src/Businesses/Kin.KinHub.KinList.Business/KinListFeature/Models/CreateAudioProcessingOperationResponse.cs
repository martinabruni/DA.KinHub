namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class CreateAudioProcessingOperationResponse
{
    public required Guid Id { get; set; }
    public required Uri UploadUrl { get; set; }
    public required DateTime UploadExpiresAt { get; set; }
    public required string BlobName { get; set; }
    public int RetryAfterSeconds { get; set; }
}
