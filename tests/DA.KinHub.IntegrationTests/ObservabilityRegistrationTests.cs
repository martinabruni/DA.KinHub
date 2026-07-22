using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace DA.KinHub.IntegrationTests;

public sealed class ObservabilityRegistrationTests
{
    [Fact]
    public void AddKinHubObservabilityDoesNotRequireAzureMonitorConnectionString()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [RuntimeOptions.SectionName + ":AppName"] = "KinHub",
            [RuntimeOptions.SectionName + ":ApiVersion"] = "1.0",
            [RuntimeOptions.SectionName + ":Environment"] = "Test"
        }).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<RuntimeOptions>().BindConfiguration(RuntimeOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<RuntimeOptions>, RuntimeOptionsValidator>();
        services.AddKinHubObservability(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.NotNull(provider.GetRequiredService<BuildInfoProvider>());
        Assert.NotNull(provider.GetRequiredService<KinHubTelemetry>());
        Assert.NotNull(provider.GetRequiredService<MeterProvider>());
        Assert.NotNull(provider.GetRequiredService<TracerProvider>());
        Assert.False(ObservabilityServiceCollectionExtensions.HasAzureMonitorConnectionString(configuration));
    }
}
