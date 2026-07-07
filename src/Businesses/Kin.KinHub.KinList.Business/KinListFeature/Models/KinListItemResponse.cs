namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListItemResponse
{
    public required Guid Id { get; init; }
    public required string Text { get; init; }
    public required string ETag { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
