using DA.KinHub.Business;
using DA.KinHub.Business.KinList;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Functions;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Middleware;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.OpenApi;
using DA.KinHub.Functions.Security;
using DA.KinHub.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
        services.AddOptions<PaginationReadOptions>().BindConfiguration(PaginationReadOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<PaginationReadOptions>, PaginationReadOptionsValidator>();

        services.AddKinHubObservability(context.Configuration);
        services.AddKinHubSecurity(context.Configuration);
        services.AddBusiness();
        services.AddInfrastructure(context.Configuration);
        services.AddSingleton<ApiProblemDetailsFactory>();
        services.AddSingleton<OpenApiDocumentProvider>();
        services.AddSingleton<JoinFamilyRateLimiter>();
    })
    .Build();

await host.RunAsync();
