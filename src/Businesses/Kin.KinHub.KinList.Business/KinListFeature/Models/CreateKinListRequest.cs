namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class CreateKinListRequest
{
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<string> Items { get; set; } = [];
}
