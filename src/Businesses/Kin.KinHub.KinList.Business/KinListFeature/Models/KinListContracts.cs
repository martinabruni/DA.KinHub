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

public sealed class ParsedKinListAudioDraft
{
    public required string Title { get; set; }
    public required IReadOnlyList<string> Items { get; set; }
    public required string DetectedLanguage { get; set; }
    public required string PromptVersion { get; set; }
}

public sealed class KinListItemResponse
{
    public required Guid Id { get; set; }
    public required string Text { get; set; }
    public required string ETag { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class KinListResponse
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string ETag { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastModifiedAt { get; set; }
}

public sealed class KinListDetailResponse
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string ETag { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public IReadOnlyList<KinListItemResponse> Items { get; set; } = [];
}

public sealed class KinListDraftFromAudioResponse
{
    public required string Title { get; set; }
    public IReadOnlyList<string> Items { get; set; } = [];
    public required string DetectedLanguage { get; set; }
    public required string PromptVersion { get; set; }
}

public sealed class KinListItemDraftProposalResponse
{
    public required string Text { get; set; }
    public bool IsSelectedByDefault { get; set; }
    public Guid? DuplicateOfItemId { get; set; }
}

public sealed class KinListExistingDuplicateResponse
{
    public required Guid ItemId { get; set; }
    public required string Text { get; set; }
    public bool IsCompleted { get; set; }
}

public sealed class KinListItemDraftsFromAudioResponse
{
    public IReadOnlyList<KinListItemDraftProposalResponse> Items { get; set; } = [];
    public IReadOnlyList<KinListExistingDuplicateResponse> ExistingDuplicates { get; set; } = [];
    public required string DetectedLanguage { get; set; }
    public required string PromptVersion { get; set; }
}
