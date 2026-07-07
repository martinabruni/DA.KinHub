namespace Kin.KinHub.KinList.PostgreSql;

public sealed class KinListItemEntity
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid Version { get; set; }
    public bool IsCompleted { get; set; }
    public long ActivationOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public KinListEntity? List { get; set; }
}
