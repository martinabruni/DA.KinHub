namespace Kin.KinHub.KinList.Business.KinListFeature;

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
