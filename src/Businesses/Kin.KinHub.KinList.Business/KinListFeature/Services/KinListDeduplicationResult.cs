namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListDeduplicationResult
{
    public IReadOnlyList<KinListItemDraftProposalResponse> Proposals { get; init; } = [];
    public IReadOnlyList<KinListExistingDuplicateResponse> ExistingDuplicates { get; init; } = [];
}
