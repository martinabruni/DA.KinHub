namespace Kin.KinHub.KinList.PostgreSql;

public sealed class AudioProcessingOperationEntity
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public int Type { get; set; }
    public Guid? ListId { get; set; }
    public int Status { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
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
