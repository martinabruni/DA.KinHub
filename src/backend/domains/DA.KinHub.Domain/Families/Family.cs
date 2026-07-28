namespace DA.KinHub.Domain.Families;

public sealed class Family
{
    private Family()
    {
    }

    private Family(Guid id, FamilyName name, Guid createdByApplicationUserId, DateTimeOffset createdAt)
    {
        if (createdByApplicationUserId == Guid.Empty)
        {
            throw new DA.KinHub.Domain.Common.DomainException("Created by application user ID is required.");
        }

        Id = id;
        Name = name;
        CreatedByApplicationUserId = createdByApplicationUserId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public FamilyName Name { get; private set; } = null!;

    public Guid CreatedByApplicationUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? InactiveAt { get; private set; }

    public bool IsActive => InactiveAt is null;

    public static Family Create(FamilyName name, Guid createdByApplicationUserId, DateTimeOffset createdAt) =>
        new(Guid.NewGuid(), name, createdByApplicationUserId, createdAt);
}
