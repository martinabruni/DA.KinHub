namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class CreateKinListRequest
{
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<string> Items { get; set; } = [];
}

public sealed class UpdateKinListRequest
{
    public string Title { get; set; } = string.Empty;
}

public sealed class CreateKinListItemRequest
{
    public string Text { get; set; } = string.Empty;
}

public sealed class UpdateKinListItemRequest
{
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}

public sealed class KinListAudioCommand
{
    public required byte[] AudioBytes { get; set; }
    public required string ContentType { get; set; }
    public string FileName { get; set; } = "audio";
}

public sealed class CreateAudioProcessingOperationRequest
{
    public required string Type { get; set; }
    public required string ContentType { get; set; }
    public long DeclaredByteSize { get; set; }
    public Guid? ListId { get; set; }
}

public sealed class CompleteAudioProcessingOperationUploadRequest
{
}

public sealed class CreateAudioProcessingOperationResponse
{
    public required Guid Id { get; set; }
    public required Uri UploadUrl { get; set; }
    public required DateTime UploadExpiresAt { get; set; }
    public required string BlobName { get; set; }
    public int RetryAfterSeconds { get; set; }
}

public sealed class AudioProcessingOperationResponse
{
    public required Guid Id { get; set; }
    public required string Type { get; set; }
    public required string Status { get; set; }
    public Guid? ListId { get; set; }
    public string? Title { get; set; }
    public IReadOnlyList<string> Items { get; set; } = [];
    public IReadOnlyList<KinListItemDraftProposalResponse> ItemProposals { get; set; } = [];
    public IReadOnlyList<KinListExistingDuplicateResponse> ExistingDuplicates { get; set; } = [];
    public string? DetectedLanguage { get; set; }
    public string? PromptVersion { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryAfterSeconds { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public sealed class ParsedKinListAudioDraft
{
    public required string Title { get; set; }
    public required IReadOnlyList<string> Items { get; set; }
    public required string DetectedLanguage { get; set; }
    public required string PromptVersion { get; set; }
}

public sealed class KinListItemResponse
{
    public required Guid Id { get; init; }
    public required string Text { get; init; }
    public required string ETag { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class KinListResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string ETag { get; init; }
    public int TotalItems { get; init; }
    public int CompletedItems { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime LastModifiedAt { get; init; }
}

public sealed class KinListDetailResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string ETag { get; init; }
    public int TotalItems { get; init; }
    public int CompletedItems { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime LastModifiedAt { get; init; }
    public IReadOnlyList<KinListItemResponse> Items { get; init; } = [];
}

public sealed class KinListDraftFromAudioResponse
{
    public required string Title { get; init; }
    public IReadOnlyList<string> Items { get; init; } = [];
    public required string DetectedLanguage { get; init; }
    public required string PromptVersion { get; init; }
}

public sealed class KinListItemDraftProposalResponse
{
    public required string Text { get; init; }
    public bool IsSelectedByDefault { get; init; }
    public Guid? DuplicateOfItemId { get; init; }
}

public sealed class KinListExistingDuplicateResponse
{
    public required Guid ItemId { get; init; }
    public required string Text { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class KinListItemDraftsFromAudioResponse
{
    public IReadOnlyList<KinListItemDraftProposalResponse> Items { get; init; } = [];
    public IReadOnlyList<KinListExistingDuplicateResponse> ExistingDuplicates { get; init; } = [];
    public required string DetectedLanguage { get; init; }
    public required string PromptVersion { get; init; }
}
