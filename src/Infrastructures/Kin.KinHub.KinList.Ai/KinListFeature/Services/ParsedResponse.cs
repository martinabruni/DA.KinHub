namespace Kin.KinHub.KinList.Ai.KinListFeature;

internal sealed class ParsedResponse
{
    public string? Title { get; set; }
    public IReadOnlyList<string> Items { get; set; } = [];
}
