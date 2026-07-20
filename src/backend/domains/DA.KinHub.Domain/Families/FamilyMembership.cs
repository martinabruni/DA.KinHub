using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.Families;

public sealed class FamilyMembership
{
    private FamilyMembership()
    {
    }

    private FamilyMembership(Guid id, Guid applicationUserId, Guid familyId, DateTimeOffset createdAt)
    {
        if (applicationUserId == Guid.Empty)
        {
            throw new DomainException("Application user ID is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainException("Family ID is required.");
        }

        Id = id;
        ApplicationUserId = applicationUserId;
        FamilyId = familyId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ApplicationUserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? InactiveAt { get; private set; }

    public bool IsActive => InactiveAt is null;

    public static FamilyMembership Create(Guid applicationUserId, Guid familyId, DateTimeOffset createdAt) =>
        new(Guid.NewGuid(), applicationUserId, familyId, createdAt);
}
