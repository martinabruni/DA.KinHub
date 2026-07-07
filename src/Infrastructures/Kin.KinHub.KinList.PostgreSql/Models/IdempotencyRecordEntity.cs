namespace Kin.KinHub.KinList.PostgreSql;

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
