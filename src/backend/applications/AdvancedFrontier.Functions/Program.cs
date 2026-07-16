using AdvancedFrontier.Business;
using AdvancedFrontier.Functions.Configuration;
using AdvancedFrontier.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
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
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddOptions<RuntimeOptions>().BindConfiguration(RuntimeOptions.SectionName).ValidateOnStart();
        services.AddOptions<EntraOptions>().BindConfiguration(EntraOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<RuntimeOptions>, RuntimeOptionsValidator>();
        services.AddSingleton<IValidateOptions<EntraOptions>, EntraOptionsValidator>();

        var entra = context.Configuration.GetSection(EntraOptions.SectionName).Get<EntraOptions>() ?? new EntraOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = $"{entra.Instance.TrimEnd('/')}/{entra.TenantId}/v2.0";
            options.Audience = entra.Audience;
            options.RequireHttpsMetadata = true;
        });
        services.AddAuthorizationBuilder().AddPolicy(ApiAuthorization.PolicyName, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(auth => auth.User.Claims
                .Where(claim => claim.Type is "scp" or "http://schemas.microsoft.com/identity/claims/scope")
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains(entra.Scope, StringComparer.Ordinal)));

        services.AddBusiness();
        services.AddInfrastructure(context.Configuration);
        services.AddSingleton<BuildInfoProvider>();
        services.AddSingleton<ApiAuthorization>();
    })
    .Build();

await host.RunAsync();
