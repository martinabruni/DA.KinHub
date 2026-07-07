namespace Kin.KinHub.KinList.AzureOpenAi.KinListFeature;

internal sealed class ParsedResponse
{
    public string? Title { get; set; }
    public IReadOnlyList<string> Items { get; set; } = [];
}
