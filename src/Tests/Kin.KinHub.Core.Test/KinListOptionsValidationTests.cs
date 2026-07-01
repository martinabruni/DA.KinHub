using Kin.KinHub.KinList.Business.Common;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// T02.5 — the configurable KinList limits must be validated at startup (via
/// <see cref="KinListOptions.Validate"/>), and the documented defaults must match the plan.
/// </summary>
public sealed class KinListOptionsValidationTests
{
    [Fact]
    public void Defaults_MatchPlannedLimits()
    {
        var options = new KinListOptions();

        Assert.Equal(60, options.MaxAudioDurationSeconds);
        Assert.Equal(10 * 1024 * 1024, options.MaxAudioBytes);
        Assert.Equal(100, options.MaxTitleLength);
        Assert.Equal(200, options.MaxItemLength);
        Assert.Equal(100, options.MaxItemsPerList);
        Assert.Equal(50, options.MaxItemsPerBulkConfirm);
        Assert.Equal(24, options.IdempotencyRetentionHours);
        Assert.Equal(3, options.TransientRetryMaxAttempts);
    }

    [Fact]
    public void Defaults_AllowWebMMp4M4aOgg()
    {
        var options = new KinListOptions();

        Assert.Contains("audio/webm", options.AllowedAudioMimeTypes);
        Assert.Contains("video/webm", options.AllowedAudioMimeTypes);
        Assert.Contains("audio/mp4", options.AllowedAudioMimeTypes);
        Assert.Contains("audio/m4a", options.AllowedAudioMimeTypes);
        Assert.Contains("audio/x-m4a", options.AllowedAudioMimeTypes);
        Assert.Contains("audio/ogg", options.AllowedAudioMimeTypes);
        Assert.DoesNotContain("audio/wav", options.AllowedAudioMimeTypes);
    }

    [Fact]
    public void Validate_WithValidDefaults_DoesNotThrow()
    {
        var exception = Record.Exception(() => new KinListOptions().Validate());

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenMaxTitleLengthNonPositive_Throws(int value)
    {
        var options = new KinListOptions { MaxTitleLength = value };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WhenMaxItemLengthNonPositive_Throws(int value)
    {
        var options = new KinListOptions { MaxItemLength = value };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenMaxItemsPerListNonPositive_Throws()
    {
        var options = new KinListOptions { MaxItemsPerList = 0 };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenBulkConfirmExceedsListCapacity_Throws()
    {
        var options = new KinListOptions { MaxItemsPerList = 10, MaxItemsPerBulkConfirm = 11 };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenIdempotencyRetentionNonPositive_Throws()
    {
        var options = new KinListOptions { IdempotencyRetentionHours = 0 };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenMaxAudioDurationNonPositive_Throws()
    {
        var options = new KinListOptions { MaxAudioDurationSeconds = 0 };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenMaxAudioBytesNonPositive_Throws()
    {
        var options = new KinListOptions { MaxAudioBytes = 0 };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenRetryAttemptsNonPositive_Throws()
    {
        var options = new KinListOptions { TransientRetryMaxAttempts = 0 };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenMaxDelayLessThanBaseDelay_Throws()
    {
        var options = new KinListOptions
        {
            TransientRetryBaseDelayMilliseconds = 1000,
            TransientRetryMaxDelayMilliseconds = 500,
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenCleanupIntervalNonPositive_Throws()
    {
        var options = new KinListOptions { IdempotencyCleanupIntervalMinutes = 0 };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenAllowedMimeTypesEmpty_Throws()
    {
        var options = new KinListOptions { AllowedAudioMimeTypes = [] };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenAllowedMimeTypesContainBlank_Throws()
    {
        var options = new KinListOptions { AllowedAudioMimeTypes = ["audio/webm", " "] };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }
}
