namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string ETag { get; init; }
    public int TotalItems { get; init; }
    public int CompletedItems { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime LastModifiedAt { get; init; }
}
