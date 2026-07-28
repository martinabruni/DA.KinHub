using DA.KinHub.Domain.Families;

namespace DA.KinHub.Domain.Tests;

public sealed class FamilyMembershipTests
{
    [Fact]
    public void NewMembershipStartsActive()
    {
        var membership = FamilyMembership.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.True(membership.IsActive);
    }

    [Fact]
    public void FamilyRequiresNameCreatorAndTimestamp()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var family = Family.Create(FamilyName.Create("Famiglia Bruni"), Guid.NewGuid(), timestamp);

        Assert.Equal("Famiglia Bruni", family.Name.Value);
        Assert.Equal(timestamp, family.CreatedAt);
        Assert.True(family.IsActive);
        Assert.NotEqual(Guid.Empty, family.CreatedByApplicationUserId);
    }
}
