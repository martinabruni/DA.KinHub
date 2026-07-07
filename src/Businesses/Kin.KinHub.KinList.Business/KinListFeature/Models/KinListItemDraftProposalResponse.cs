namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListItemDraftProposalResponse
{
    public required string Text { get; init; }
    public bool IsSelectedByDefault { get; init; }
    public Guid? DuplicateOfItemId { get; init; }
}
