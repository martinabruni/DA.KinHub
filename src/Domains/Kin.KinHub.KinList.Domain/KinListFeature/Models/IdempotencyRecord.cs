namespace Kin.KinHub.KinList.Domain.KinListFeature;

public sealed class IdempotencyRecord
{
    public required Guid Id { get; set; }
    public required string Key { get; set; }
    public required Guid FamilyId { get; set; }
    public required Guid UserId { get; set; }
    public required string RequestHash { get; set; }
    public required string ResponseJson { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
