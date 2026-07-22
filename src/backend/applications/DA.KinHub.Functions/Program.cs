using DA.KinHub.Business;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Middleware;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.OpenApi;
using DA.KinHub.Functions.Security;
using DA.KinHub.Infrastructure;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, configuration) => configuration
        .SetBasePath(context.HostingEnvironment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables())
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<CorrelationIdMiddleware>();
        builder.UseMiddleware<ExceptionHandlingMiddleware>();
        builder.UseMiddleware<KinHubAuthorizationMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<RuntimeOptions>().BindConfiguration(RuntimeOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<RuntimeOptions>, RuntimeOptionsValidator>();

        var openTelemetry = services.AddOpenTelemetry();
        openTelemetry.WithTracing(builder => builder.AddSource("KinHub"));
        openTelemetry.WithMetrics(builder => builder.AddMeter("KinHub"));
        openTelemetry.UseFunctionsWorkerDefaults().UseAzureMonitorExporter();
        services.Configure<OpenTelemetryLoggerOptions>(options => options.IncludeScopes = true);
        services.AddSingleton<IConfigureOptions<AzureMonitorExporterOptions>, AzureMonitorExporterOptionsSetup>();

        services.AddKinHubSecurity(context.Configuration);
        services.AddBusiness();
        services.AddInfrastructure(context.Configuration);
        services.AddSingleton<BuildInfoProvider>();
        services.AddSingleton<KinHubTelemetry>();
        services.AddSingleton<ApiProblemDetailsFactory>();
        services.AddSingleton<OpenApiDocumentProvider>();
    })
    .Build();

await host.RunAsync();

file sealed class AzureMonitorExporterOptionsSetup(TokenCredential credential) : IConfigureOptions<AzureMonitorExporterOptions>
{
    public void Configure(AzureMonitorExporterOptions options)
    {
        options.Credential = credential;
    }
}
