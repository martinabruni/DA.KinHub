namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListItemDraftsFromAudioResponse
{
    public IReadOnlyList<KinListItemDraftProposalResponse> Items { get; init; } = [];
    public IReadOnlyList<KinListExistingDuplicateResponse> ExistingDuplicates { get; init; } = [];
    public required string DetectedLanguage { get; init; }
    public required string PromptVersion { get; init; }
}
