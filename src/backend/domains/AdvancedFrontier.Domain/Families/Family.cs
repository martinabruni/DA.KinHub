namespace AdvancedFrontier.Domain.Families;

public sealed class Family
{
    private Family()
    {
    }

    private Family(Guid id, DateTimeOffset createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? InactiveAt { get; private set; }

    public bool IsActive => InactiveAt is null;

    public static Family Create(DateTimeOffset createdAt) => new(Guid.NewGuid(), createdAt);
}
