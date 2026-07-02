using Kin.KinHub.KinList.Ai.Common;

namespace Kin.KinHub.Core.Test;

public sealed class KinListAiOptionsTests
{
    [Fact]
    public void OpenAiOptions_WithoutEndpointAndApiKey_IsNotConfigured()
    {
        var options = new OpenAiOptions();

        Assert.False(options.IsConfigured());
        Assert.False(options.HasPartialConfiguration());
    }

    [Fact]
    public void OpenAiOptions_WithOnlyEndpoint_IsPartialConfiguration()
    {
        var options = new OpenAiOptions
        {
            Endpoint = "https://example.openai.azure.com",
        };

        Assert.False(options.IsConfigured());
        Assert.True(options.HasPartialConfiguration());
    }

    [Fact]
    public void OpenAiOptions_WithEndpointAndApiKey_IsConfigured()
    {
        var options = new OpenAiOptions
        {
            Endpoint = "https://example.openai.azure.com",
            ApiKey = "secret",
        };

        Assert.True(options.IsConfigured());
        Assert.True(options.HasPartialConfiguration());
    }

    [Fact]
    public void SpeechOptions_WithoutEndpointAndApiKey_IsNotConfigured()
    {
        var options = new SpeechToTextOptions();

        Assert.False(options.IsConfigured());
        Assert.False(options.HasPartialConfiguration());
    }

    [Fact]
    public void SpeechOptions_WithOnlyApiKey_IsPartialConfiguration()
    {
        var options = new SpeechToTextOptions
        {
            ApiKey = "secret",
        };

        Assert.False(options.IsConfigured());
        Assert.True(options.HasPartialConfiguration());
    }

    [Fact]
    public void SpeechOptions_WithEndpointAndApiKey_IsConfigured()
    {
        var options = new SpeechToTextOptions
        {
            Endpoint = "https://example.speech.azure.com",
            ApiKey = "secret",
        };

        Assert.True(options.IsConfigured());
        Assert.True(options.HasPartialConfiguration());
    }
}
