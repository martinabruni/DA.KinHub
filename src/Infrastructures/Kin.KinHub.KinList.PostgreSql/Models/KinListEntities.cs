namespace Kin.KinHub.KinList.PostgreSql.Models;

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

public sealed class IdempotencyRecordEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
