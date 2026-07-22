using DA.KinHub.Functions.Configuration;
using Azure.Core;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace DA.KinHub.Functions.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    private const string ApplicationInsightsConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    public static IServiceCollection AddKinHubObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var openTelemetry = services.AddOpenTelemetry();
        openTelemetry.WithTracing(builder => builder.AddSource("KinHub"));
        openTelemetry.WithMetrics(builder => builder.AddMeter("KinHub"));

        var workerBuilder = openTelemetry.UseFunctionsWorkerDefaults();
        if (HasAzureMonitorConnectionString(configuration))
        {
            workerBuilder.UseAzureMonitorExporter();
            services.AddSingleton<IConfigureOptions<AzureMonitorExporterOptions>, AzureMonitorExporterOptionsSetup>();
        }

        services.Configure<OpenTelemetryLoggerOptions>(options => options.IncludeScopes = true);
        services.AddSingleton<BuildInfoProvider>();
        services.AddSingleton<KinHubTelemetry>();
        return services;
    }

    public static bool HasAzureMonitorConnectionString(IConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(configuration[ApplicationInsightsConnectionStringKey]);
    }

    private sealed class AzureMonitorExporterOptionsSetup(TokenCredential credential) : IConfigureOptions<AzureMonitorExporterOptions>
    {
        public void Configure(AzureMonitorExporterOptions options)
        {
            options.Credential = credential;
        }
    }
}
