using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinList;

public sealed class KinListItem
{
    private KinListItem()
    {
    }

    private KinListItem(
        Guid id,
        Guid familyId,
        Guid registrationGroupId,
        KinListItemName name,
        int positionInGroup,
        Guid ownerApplicationUserId,
        ItemVisibility visibility,
        ItemStatus status,
        DateTimeOffset createdAt,
        long revision)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainException("Family ID is required.");
        }

        if (registrationGroupId == Guid.Empty)
        {
            throw new DomainException("Registration group ID is required.");
        }

        if (ownerApplicationUserId == Guid.Empty)
        {
            throw new DomainException("Owner application user ID is required.");
        }

        if (positionInGroup < 0)
        {
            throw new DomainException("Position in group cannot be negative.");
        }

        if (revision < 1)
        {
            throw new DomainException("Revision must be at least 1.");
        }

        Id = id;
        FamilyId = familyId;
        RegistrationGroupId = registrationGroupId;
        Name = name;
        PositionInGroup = positionInGroup;
        OwnerApplicationUserId = ownerApplicationUserId;
        Visibility = visibility;
        Status = status;
        CreatedAt = createdAt;
        Revision = revision;
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    public Guid RegistrationGroupId { get; private set; }

    public KinListItemName Name { get; private set; } = null!;

    public int PositionInGroup { get; private set; }

    public Guid OwnerApplicationUserId { get; private set; }

    public ItemVisibility Visibility { get; private set; }

    public ItemStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? ModifiedByApplicationUserId { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? CompletedByApplicationUserId { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? InactiveAt { get; private set; }

    public long Revision { get; private set; }

    public static KinListItem CreateShared(
        Guid familyId,
        Guid registrationGroupId,
        KinListItemName name,
        int positionInGroup,
        Guid ownerApplicationUserId,
        DateTimeOffset createdAt)
        => new(Guid.NewGuid(), familyId, registrationGroupId, name, positionInGroup, ownerApplicationUserId, ItemVisibility.Shared, ItemStatus.Active, createdAt, revision: 1);
}
