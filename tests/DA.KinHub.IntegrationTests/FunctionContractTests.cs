using System.Text.Json;
using DA.KinHub.Business;
using DA.KinHub.Business.Projects;
using DA.KinHub.Domain.Documents;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Functions;
using DA.KinHub.Functions.Http;
using DA.KinHub.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DA.KinHub.IntegrationTests;

public sealed class FunctionContractTests
{
    [Fact]
    public void VersionAndStatusEndpointsReturnBuildMetadata()
    {
        var provider = new BuildInfoProvider(Options.Create(new RuntimeOptions { AppName = "KinHub", ApiVersion = "1.0", Environment = "Test" }));
        var functions = new MetadataFunctions(provider, TimeProvider.System, Options.Create(new EntraOptions
        {
            Enabled = true,
            Instance = "https://login.microsoftonline.com",
            TenantId = "contoso.onmicrosoft.com",
            Audience = "api://kinhub-test",
            Scope = "access_as_user"
        }));

        var versionResult = Assert.IsType<OkObjectResult>(functions.Version(Request("/api/version")));
        var statusResult = Assert.IsType<OkObjectResult>(functions.Status(Request("/api/status")));

        var version = Assert.IsType<BuildInfo>(versionResult.Value);
        Assert.Equal("KinHub", version.AppName);
        Assert.Equal("1.0", version.ApiVersion);
        Assert.NotNull(statusResult.Value);

        var openApiResult = Assert.IsType<OkObjectResult>(functions.OpenApi(Request("/api/openapi.json")));
        var openApi = JsonSerializer.Serialize(openApiResult.Value);
        Assert.Contains("https://login.microsoftonline.com/contoso.onmicrosoft.com/oauth2/v2.0/authorize", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("https://https://", openApi, StringComparison.Ordinal);
        Assert.Contains("/api/kinlist/bootstrap", openApi, StringComparison.Ordinal);
        Assert.Contains("/api/kinlist/family-context", openApi, StringComparison.Ordinal);
    }

    [Fact]
    public void ProblemDetailsUsesStandardMediaTypeAndExtensions()
    {
        var result = ApiResults.Problem(Request("/api/kinlist/bootstrap"), 400, "Invalid", "Invalid input", "request.invalid");

        var problem = Assert.IsType<ProblemDetails>(result.Value);
        var json = JsonSerializer.Serialize(problem);
        Assert.Equal("application/problem+json", Assert.Single(result.ContentTypes));
        Assert.Contains("request.invalid", json, StringComparison.Ordinal);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public void CriticalEntraConfigurationRejectsPlaceholdersWhenEnabled()
    {
        var validator = new EntraOptionsValidator();

        var result = validator.Validate(null, new EntraOptions
        {
            Enabled = true,
            TenantId = "<ENTRA_TENANT_ID>",
            Audience = "<AUDIENCE>",
            Scope = "<SCOPE>"
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void DependencyInjectionRegistersBusinessAndInfrastructureServices()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSql"] = "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub",
            ["Database:ApplyMigrationsOnStartup"] = "false",
            ["Storage:AccountUri"] = "https://kinhubtest.blob.core.windows.net/",
            ["Storage:ContainerName"] = "documents"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBusiness();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IProjectService>());
        Assert.NotNull(scope.ServiceProvider.GetService<DA.KinHub.Business.Identity.IKinListBootstrapService>());
        Assert.NotNull(scope.ServiceProvider.GetService<DA.KinHub.Business.Identity.IFamilyAccessService>());
        Assert.NotNull(scope.ServiceProvider.GetService<IDocumentStorage>());
        Assert.NotNull(scope.ServiceProvider.GetService<DA.KinHub.Infrastructure.Persistence.KinHubDbContext>());
    }

    private static HttpRequest Request(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context.Request;
    }
}
