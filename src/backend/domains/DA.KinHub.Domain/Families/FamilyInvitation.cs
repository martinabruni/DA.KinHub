using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.Families;

public sealed class FamilyInvitation
{
    private FamilyInvitation()
    {
    }

    private FamilyInvitation(
        Guid id,
        Guid familyId,
        Guid createdByApplicationUserId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        byte[] codeHmac,
        string hmacKeyVersion,
        DateTimeOffset? revokedAt,
        DateTimeOffset? consumedAt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Invitation ID is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainException("Family ID is required.");
        }

        if (createdByApplicationUserId == Guid.Empty)
        {
            throw new DomainException("Creator application user ID is required.");
        }

        if (expiresAt <= createdAt)
        {
            throw new DomainException("Invitation expiration must be after creation.");
        }

        if (codeHmac.Length == 0)
        {
            throw new DomainException("Invitation HMAC is required.");
        }

        if (string.IsNullOrWhiteSpace(hmacKeyVersion))
        {
            throw new DomainException("Invitation HMAC key version is required.");
        }

        if (revokedAt is not null && revokedAt < createdAt)
        {
            throw new DomainException("Invitation revocation cannot precede creation.");
        }

        if (consumedAt is not null && consumedAt < createdAt)
        {
            throw new DomainException("Invitation consumption cannot precede creation.");
        }

        Id = id;
        FamilyId = familyId;
        CreatedByApplicationUserId = createdByApplicationUserId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        CodeHmac = codeHmac;
        HmacKeyVersion = hmacKeyVersion;
        RevokedAt = revokedAt;
        ConsumedAt = consumedAt;
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    public Guid CreatedByApplicationUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public byte[] CodeHmac { get; private set; } = [];

    public string HmacKeyVersion { get; private set; } = string.Empty;

    public DateTimeOffset? RevokedAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsActiveAt(DateTimeOffset nowUtc) => RevokedAt is null && ConsumedAt is null && ExpiresAt > nowUtc;

    public static FamilyInvitation CreateStored(
        Guid id,
        Guid familyId,
        Guid createdByApplicationUserId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        byte[] codeHmac,
        string hmacKeyVersion,
        DateTimeOffset? revokedAt = null,
        DateTimeOffset? consumedAt = null) =>
        new(id, familyId, createdByApplicationUserId, createdAt, expiresAt, codeHmac, hmacKeyVersion, revokedAt, consumedAt);

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
        {
            throw new DomainException("The invitation is already revoked.");
        }

        if (ConsumedAt is not null)
        {
            throw new DomainException("The invitation has already been consumed.");
        }

        if (revokedAt < CreatedAt)
        {
            throw new DomainException("Invitation revocation cannot precede creation.");
        }

        RevokedAt = revokedAt;
    }

    public void Consume(DateTimeOffset consumedAt)
    {
        if (ConsumedAt is not null)
        {
            throw new DomainException("The invitation has already been consumed.");
        }

        if (RevokedAt is not null)
        {
            throw new DomainException("The invitation has already been revoked.");
        }

        if (consumedAt < CreatedAt)
        {
            throw new DomainException("Invitation consumption cannot precede creation.");
        }

        ConsumedAt = consumedAt;
    }
}
