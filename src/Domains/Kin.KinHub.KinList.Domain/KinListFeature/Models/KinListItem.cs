namespace Kin.KinHub.KinList.Domain.KinListFeature;

public sealed class KinListItem
{
    public required Guid Id { get; set; }
    public required Guid ListId { get; set; }
    public required string Text { get; set; }
    public required Guid Version { get; set; }
    public bool IsCompleted { get; set; }
    public long ActivationOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
