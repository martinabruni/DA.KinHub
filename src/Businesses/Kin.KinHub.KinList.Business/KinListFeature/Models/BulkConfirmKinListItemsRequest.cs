namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class BulkConfirmKinListItemsRequest
{
    public IReadOnlyList<string> Items { get; set; } = [];
}
