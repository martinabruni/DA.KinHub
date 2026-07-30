using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinServices;

public sealed class FamilyKinServiceAvailability
{
    private FamilyKinServiceAvailability()
    {
    }

    private FamilyKinServiceAvailability(Guid id, Guid familyId, Guid kinServiceId, bool isActive, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Family KinService availability ID is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainException("Family ID is required.");
        }

        if (kinServiceId == Guid.Empty)
        {
            throw new DomainException("KinService ID is required.");
        }

        Id = id;
        FamilyId = familyId;
        KinServiceId = kinServiceId;
        IsActive = isActive;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    public Guid KinServiceId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static FamilyKinServiceAvailability Create(Guid familyId, Guid kinServiceId, bool isActive, DateTimeOffset createdAt)
        => new(Guid.NewGuid(), familyId, kinServiceId, isActive, createdAt);
}
