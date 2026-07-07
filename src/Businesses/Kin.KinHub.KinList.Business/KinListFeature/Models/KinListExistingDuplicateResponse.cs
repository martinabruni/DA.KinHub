namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListExistingDuplicateResponse
{
    public required Guid ItemId { get; init; }
    public required string Text { get; init; }
    public bool IsCompleted { get; init; }
}
