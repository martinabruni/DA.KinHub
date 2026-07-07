namespace Kin.KinHub.KinList.Domain.KinListFeature;

public sealed class AudioProcessingOperation
{
    public required Guid Id { get; set; }
    public required Guid FamilyId { get; set; }
    public required Guid UserId { get; set; }
    public required AudioProcessingOperationType Type { get; set; }
    public Guid? ListId { get; set; }
    public required AudioProcessingOperationStatus Status { get; set; }
    public required string BlobName { get; set; }
    public required string ContentType { get; set; }
    public long DeclaredByteSize { get; set; }
    public long? UploadedByteSize { get; set; }
    public string? Title { get; set; }
    public string ProposedItemsJson { get; set; } = "[]";
    public string? DetectedLanguage { get; set; }
    public string? PromptVersion { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UploadCompletedAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
}
