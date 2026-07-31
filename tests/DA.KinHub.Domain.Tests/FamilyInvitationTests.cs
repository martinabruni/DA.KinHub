using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;

namespace DA.KinHub.Domain.Tests;

public sealed class FamilyInvitationTests
{
    [Fact]
    public void StoredInvitationIsActiveOnlyBeforeExpiration()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var invitation = FamilyInvitation.CreateStored(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            createdAt,
            createdAt.AddHours(1),
            [1, 2, 3],
            "v1");

        Assert.True(invitation.IsActiveAt(createdAt.AddMinutes(30)));
        Assert.False(invitation.IsActiveAt(createdAt.AddHours(1)));
    }

    [Fact]
    public void InvalidExpirationIsRejected()
    {
        var createdAt = DateTimeOffset.UtcNow;

        Assert.Throws<DomainException>(() => FamilyInvitation.CreateStored(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            createdAt,
            createdAt,
            [1],
            "v1"));
    }
}
