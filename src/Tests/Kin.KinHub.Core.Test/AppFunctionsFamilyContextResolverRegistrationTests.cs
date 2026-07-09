using Kin.KinHub.App.Functions.Common.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Guards the single highest-risk regression from merging AddKinHubKinListApi/AddKinHubKinRecipeApi
/// into App.Functions/ServiceCollectionExtensions.cs: KinRecipe must register after KinList so that
/// IFamilyContextResolver resolves to RemoteFamilyOwnershipService (KinRecipe's registration), not
/// RemoteFamilyContextResolver (KinList's registration) — matching pre-merge behavior.
/// </summary>
public sealed class AppFunctionsFamilyContextResolverRegistrationTests
{
    [Fact]
    public void AddKinHubAppFunctions_ResolvesIFamilyContextResolver_AsRemoteFamilyOwnershipService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KinHub"] = "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub",
                ["Jwt:Issuer"] = "http://localhost",
                ["Jwt:Secret"] = "development-only-kinhub-jwt-secret-0001",
                ["Jwt:Audience"] = "kinhub.api",
                ["FamilyContextApi:BaseUrl"] = "http://localhost:5001",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddKinHubAppFunctions(configuration, new FakeHostEnvironment());

        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IFamilyContextResolver>();

        Assert.IsType<Kin.KinHub.App.Functions.Common.RemoteFamilyOwnershipService>(resolver);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Kin.KinHub.App.Functions";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
