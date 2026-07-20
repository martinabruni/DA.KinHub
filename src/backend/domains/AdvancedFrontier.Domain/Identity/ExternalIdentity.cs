using AdvancedFrontier.Domain.Common;

namespace AdvancedFrontier.Domain.Identity;

public readonly record struct ExternalIdentity
{
    public ExternalIdentity(string issuer, Guid objectId)
    {
        var normalizedIssuer = issuer?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedIssuer))
        {
            throw new DomainException("External issuer is required.");
        }

        if (objectId == Guid.Empty)
        {
            throw new DomainException("External object ID is required.");
        }

        Issuer = normalizedIssuer;
        ObjectId = objectId;
    }

    public string Issuer { get; }

    public Guid ObjectId { get; }
}
