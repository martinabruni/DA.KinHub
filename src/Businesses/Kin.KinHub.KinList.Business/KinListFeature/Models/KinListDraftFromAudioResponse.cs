namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListDraftFromAudioResponse
{
    public required string Title { get; init; }
    public IReadOnlyList<string> Items { get; init; } = [];
    public required string DetectedLanguage { get; init; }
    public required string PromptVersion { get; init; }
}
