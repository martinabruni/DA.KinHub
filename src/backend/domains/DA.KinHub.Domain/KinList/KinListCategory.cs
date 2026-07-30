using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinList;

public sealed class KinListCategory
{
    private KinListCategory()
    {
    }

    private KinListCategory(Guid id, Guid familyId, KinListCategoryName name, Guid createdByApplicationUserId, DateTimeOffset createdAt)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainException("Family ID is required.");
        }

        if (createdByApplicationUserId == Guid.Empty)
        {
            throw new DomainException("Created by application user ID is required.");
        }

        Id = id;
        FamilyId = familyId;
        Name = name.Value;
        NormalizedName = name.NormalizedValue;
        CreatedByApplicationUserId = createdByApplicationUserId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public Guid CreatedByApplicationUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? InactiveAt { get; private set; }

    public static KinListCategory Create(Guid familyId, KinListCategoryName name, Guid createdByApplicationUserId, DateTimeOffset createdAt)
        => new(Guid.NewGuid(), familyId, name, createdByApplicationUserId, createdAt);
}
