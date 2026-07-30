using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.KinServices;

namespace DA.KinHub.Domain.Tests;

public sealed class KinServiceTests
{
    [Fact]
    public void CreateRejectsEmptyKey()
    {
        var exception = Assert.Throws<DomainException>(() => KinService.Create(Guid.NewGuid(), "   ", "/kinlist", true, true, DateTimeOffset.UtcNow));

        Assert.Equal("KinService key is required.", exception.Message);
    }

    [Fact]
    public void LocalizationNormalizesLanguageToLowercase()
    {
        var localization = KinServiceLocalization.Create(Guid.NewGuid(), Guid.NewGuid(), "EN", "KinList", "Shared list", DateTimeOffset.UtcNow);

        Assert.Equal("en", localization.Language);
    }

    [Fact]
    public void LocalizationRejectsUnsupportedLanguage()
    {
        var exception = Assert.Throws<DomainException>(() => KinServiceLocalization.Create(Guid.NewGuid(), Guid.NewGuid(), "fr", "KinList", "Shared list", DateTimeOffset.UtcNow));

        Assert.Equal("KinService language is not supported.", exception.Message);
    }

    [Fact]
    public void AvailabilityRejectsEmptyFamilyId()
    {
        var exception = Assert.Throws<DomainException>(() => FamilyKinServiceAvailability.Create(Guid.Empty, Guid.NewGuid(), true, DateTimeOffset.UtcNow));

        Assert.Equal("Family ID is required.", exception.Message);
    }
}
