using System.Collections.Immutable;
using System.Text.Json;
using DA.KinHub.Business;
using DA.KinHub.Business.Common;
using DA.KinHub.Domain.Documents;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Functions;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.OpenApi;
using DA.KinHub.Functions.Security;
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
        var openApiProvider = new OpenApiDocumentProvider(provider, Options.Create(new EntraOptions
        {
            Enabled = true,
            Instance = "https://login.microsoftonline.com",
            TenantId = "contoso.onmicrosoft.com",
            Audience = "api://kinhub-test",
            Scope = "access_as_user"
        }));
        var functions = new MetadataFunctions(provider, TimeProvider.System, openApiProvider);

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
        var request = Request("/api/kinlist/bootstrap");
        ApiResults.EnsureCorrelationId(request.HttpContext);
        var result = new ApiProblemDetailsFactory().Create(request.HttpContext, 400, "Invalid", "Invalid input", "request.invalid");

        var problem = Assert.IsType<ProblemDetails>(result.Value);
        var json = JsonSerializer.Serialize(problem);
        Assert.Equal(ApiResults.ProblemMediaType, Assert.Single(result.ContentTypes));
        Assert.Contains("request.invalid", json, StringComparison.Ordinal);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
        Assert.True(problem.Extensions.ContainsKey("correlationId"));
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
            ["Database:Mode"] = "ConnectionString",
            ["Database:ConnectionString"] = "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub",
            ["Database:ApplyMigrationsOnStartup"] = "false",
            ["Storage:AccountUri"] = "https://kinhubtest.blob.core.windows.net/",
            ["Storage:ContainerName"] = "documents"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new HostingEnvironmentStub(isDevelopment: true));
        services.AddBusiness();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<DA.KinHub.Business.Identity.IKinListBootstrapService>());
        Assert.NotNull(scope.ServiceProvider.GetService<DA.KinHub.Business.Identity.IFamilyAccessService>());
        Assert.NotNull(scope.ServiceProvider.GetService<IDocumentStorage>());
        Assert.NotNull(scope.ServiceProvider.GetService<DA.KinHub.Infrastructure.Persistence.KinHubDbContext>());
    }

    [Fact]
    public void DatabaseOptionsRejectConnectionStringOutsideDevelopment()
    {
        var validator = new DA.KinHub.Infrastructure.Persistence.DatabaseOptionsValidator(new HostingEnvironmentStub(isDevelopment: false));

        var result = validator.Validate(null, new DA.KinHub.Infrastructure.Persistence.DatabaseOptions
        {
            Mode = "ConnectionString",
            ConnectionString = "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub"
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void DatabaseOptionsAcceptManagedIdentityOutsideDevelopment()
    {
        var validator = new DA.KinHub.Infrastructure.Persistence.DatabaseOptionsValidator(new HostingEnvironmentStub(isDevelopment: false));

        var result = validator.Validate(null, new DA.KinHub.Infrastructure.Persistence.DatabaseOptions
        {
            Mode = "ManagedIdentity",
            Host = "kinhub.postgres.database.azure.com",
            DatabaseName = "kinhub",
            Username = "kinhub-runtime",
            RequireSsl = true
        });

        Assert.False(result.Failed);
    }

    [Fact]
    public void FunctionMetadataDefaultsToApiAccessAndRecognizesMarkers()
    {
        var provider = new FunctionAccessMetadataProvider();

        var bootstrap = provider.Get(Definition("DA.KinHub.Functions.Functions.KinListBootstrapFunctions.Bootstrap"));
        var family = provider.Get(Definition("DA.KinHub.Functions.Functions.KinListFamilyFunctions.FamilyContext"));
        var version = provider.Get(Definition("DA.KinHub.Functions.Functions.MetadataFunctions.Version"));

        Assert.True(bootstrap.IsHttp);
        Assert.False(bootstrap.AllowAnonymous);
        Assert.False(bootstrap.RequiresFamilyAccess);
        Assert.True(family.RequiresFamilyAccess);
        Assert.True(version.AllowAnonymous);
    }

    [Fact]
    public void EntraValidatorRejectsNonHttpsInstanceWhenEnabled()
    {
        var validator = new EntraOptionsValidator();

        var result = validator.Validate(null, new EntraOptions
        {
            Enabled = true,
            Instance = "http://login.microsoftonline.com",
            TenantId = "contoso.onmicrosoft.com",
            Audience = "api://kinhub-test",
            Scope = "access_as_user"
        });

        Assert.True(result.Failed);
    }

    private static HttpRequest Request(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context.Request;
    }

    private static Microsoft.Azure.Functions.Worker.FunctionDefinition Definition(string entryPoint)
    {
        return new StubFunctionDefinition(entryPoint);
    }

    private sealed class StubFunctionDefinition(string entryPoint) : Microsoft.Azure.Functions.Worker.FunctionDefinition
    {
        public override ImmutableArray<Microsoft.Azure.Functions.Worker.FunctionParameter> Parameters => ImmutableArray<Microsoft.Azure.Functions.Worker.FunctionParameter>.Empty;
        public override string PathToAssembly => typeof(MetadataFunctions).Assembly.Location;
        public override string EntryPoint => entryPoint;
        public override string Id => entryPoint;
        public override string Name => entryPoint;
        public override IImmutableDictionary<string, Microsoft.Azure.Functions.Worker.BindingMetadata> InputBindings => ImmutableDictionary<string, Microsoft.Azure.Functions.Worker.BindingMetadata>.Empty;
        public override IImmutableDictionary<string, Microsoft.Azure.Functions.Worker.BindingMetadata> OutputBindings => ImmutableDictionary<string, Microsoft.Azure.Functions.Worker.BindingMetadata>.Empty;
    }

    private sealed class HostingEnvironmentStub(bool isDevelopment) : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = isDevelopment ? Microsoft.Extensions.Hosting.Environments.Development : Microsoft.Extensions.Hosting.Environments.Production;
        public string ApplicationName { get; set; } = "KinHub.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
