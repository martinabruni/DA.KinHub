using AdvancedFrontier.Domain.Common;
using AdvancedFrontier.Domain.Identity;

namespace AdvancedFrontier.Domain.Tests;

public sealed class ExternalIdentityTests
{
    [Fact]
    public void EmptyIssuerIsRejected()
    {
        Assert.Throws<DomainException>(() => new ExternalIdentity(" ", Guid.NewGuid()));
    }

    [Fact]
    public void EmptyObjectIdIsRejected()
    {
        Assert.Throws<DomainException>(() => new ExternalIdentity("https://issuer.example", Guid.Empty));
    }
}
