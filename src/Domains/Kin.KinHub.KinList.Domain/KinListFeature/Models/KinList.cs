namespace Kin.KinHub.KinList.Domain.KinListFeature;

public sealed class KinList
{
    public required Guid Id { get; set; }
    public required Guid FamilyId { get; set; }
    public required string Title { get; set; }
    public required Guid Version { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
}
