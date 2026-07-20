using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.Identity;

public sealed class ApplicationUser
{
    private ApplicationUser()
    {
    }

    private ApplicationUser(Guid id, string externalIssuer, Guid externalObjectId, DateTimeOffset createdAt)
    {
        Id = id;
        ExternalIssuer = externalIssuer;
        ExternalObjectId = externalObjectId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string ExternalIssuer { get; private set; } = string.Empty;

    public Guid ExternalObjectId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? InactiveAt { get; private set; }

    public bool IsActive => InactiveAt is null;

    public ExternalIdentity ExternalIdentity => new(ExternalIssuer, ExternalObjectId);

    public static ApplicationUser Create(ExternalIdentity externalIdentity, DateTimeOffset createdAt) =>
        new(Guid.NewGuid(), externalIdentity.Issuer, externalIdentity.ObjectId, createdAt);

    public void Deactivate(DateTimeOffset inactiveAt)
    {
        if (InactiveAt is not null)
        {
            throw new DomainException("The application user is already inactive.");
        }

        InactiveAt = inactiveAt;
    }
}
