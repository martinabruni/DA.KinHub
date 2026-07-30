using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinList;

public sealed class RegistrationGroup
{
    private RegistrationGroup()
    {
    }

    private RegistrationGroup(Guid id, Guid familyId, Guid recordingId, Guid createdByApplicationUserId, DateTimeOffset createdAt)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainException("Family ID is required.");
        }

        if (recordingId == Guid.Empty)
        {
            throw new DomainException("Recording ID is required.");
        }

        if (createdByApplicationUserId == Guid.Empty)
        {
            throw new DomainException("Created by application user ID is required.");
        }

        Id = id;
        FamilyId = familyId;
        RecordingId = recordingId;
        CreatedByApplicationUserId = createdByApplicationUserId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    public Guid RecordingId { get; private set; }

    public Guid CreatedByApplicationUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? InactiveAt { get; private set; }

    public static RegistrationGroup Create(Guid familyId, Guid recordingId, Guid createdByApplicationUserId, DateTimeOffset createdAt)
        => new(Guid.NewGuid(), familyId, recordingId, createdByApplicationUserId, createdAt);
}
