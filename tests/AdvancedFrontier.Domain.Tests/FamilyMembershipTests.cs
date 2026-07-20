using AdvancedFrontier.Domain.Families;

namespace AdvancedFrontier.Domain.Tests;

public sealed class FamilyMembershipTests
{
    [Fact]
    public void NewMembershipStartsActive()
    {
        var membership = FamilyMembership.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.True(membership.IsActive);
    }
}
