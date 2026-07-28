using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;

namespace DA.KinHub.Domain.Tests;

public sealed class FamilyNameTests
{
    [Fact]
    public void CreateNormalizesWhitespace()
    {
        var name = FamilyName.Create("  Casa   Bruni\tFamiglia  ");

        Assert.Equal("Casa Bruni Famiglia", name.Value);
    }

    [Fact]
    public void CreateRejectsInvalidValues()
    {
        Assert.Throws<DomainException>(() => FamilyName.Create("   "));
        Assert.Throws<DomainException>(() => FamilyName.Create(new string('a', 101)));
        Assert.Throws<DomainException>(() => FamilyName.Create("Casa\u0001Bruni"));
    }

    [Fact]
    public void CreatePreservesUnicodeAndPunctuation()
    {
        var name = FamilyName.Create("L'Équipe di Željka");

        Assert.Equal("L'Équipe di Željka", name.Value);
    }
}
