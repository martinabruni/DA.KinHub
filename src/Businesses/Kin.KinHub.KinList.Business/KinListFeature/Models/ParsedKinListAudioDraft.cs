namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class ParsedKinListAudioDraft
{
    public required string Title { get; set; }
    public required IReadOnlyList<string> Items { get; set; }
    public required string DetectedLanguage { get; set; }
    public required string PromptVersion { get; set; }
}
