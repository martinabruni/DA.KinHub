namespace Kin.KinHub.KinList.PostgreSql.Models;

public sealed class KinListEntity
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid Version { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public ICollection<KinListItemEntity> Items { get; set; } = [];
}

public sealed class KinListItemEntity
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid Version { get; set; }
    public bool IsCompleted { get; set; }
    public long ActivationOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public KinListEntity? List { get; set; }
}

public sealed class IdempotencyRecordEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

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
