using AdvancedFrontier.Functions.Security;
using System.Security.Claims;

namespace AdvancedFrontier.IntegrationTests;

public sealed class ExternalIdentityClaimsResolverTests
{
    [Fact]
    public void MissingClaimsFailClosed()
    {
        var resolver = new ExternalIdentityClaimsResolver();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "Kin Hub")], "Bearer"));

        Assert.False(resolver.TryResolve(principal, out _));
    }

    [Fact]
    public void IssuerAndObjectIdResolve()
    {
        var objectId = Guid.NewGuid();
        var resolver = new ExternalIdentityClaimsResolver();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("iss", "https://issuer.example"),
            new Claim("oid", objectId.ToString())
        ], "Bearer"));

        Assert.True(resolver.TryResolve(principal, out var externalIdentity));
        Assert.Equal("https://issuer.example", externalIdentity.Issuer);
        Assert.Equal(objectId, externalIdentity.ObjectId);
    }
}
