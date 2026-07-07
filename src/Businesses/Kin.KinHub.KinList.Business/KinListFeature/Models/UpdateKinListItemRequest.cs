namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class UpdateKinListItemRequest
{
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
