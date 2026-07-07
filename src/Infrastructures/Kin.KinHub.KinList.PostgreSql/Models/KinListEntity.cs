namespace Kin.KinHub.KinList.PostgreSql;

public sealed class KinListEntity
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid Version { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public ICollection<KinListItemEntity> Items { get; set; } = [];
}
