using Kin.KinHub.App.Functions.Common;
using Kin.KinHub.App.Functions.Common.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Guards the Identity/Core boundary: App.Functions now owns Core.PostgreSql directly, so
/// IFamilyContextResolver must resolve to the in-process CoreFamilyContextResolver — not a
/// remote HTTP-based resolver — regardless of which module (KinList/KinRecipe) registers last.
/// </summary>
public sealed class AppFunctionsFamilyContextResolverRegistrationTests
{
    [Fact]
    public void AddKinHubAppFunctions_ResolvesIFamilyContextResolver_AsCoreFamilyContextResolver()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KinHub"] = "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub",
                ["Jwt:Issuer"] = "http://localhost",
                ["Jwt:Secret"] = "development-only-kinhub-jwt-secret-0001",
                ["Jwt:Audience"] = "kinhub.api",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddKinHubAppFunctions(configuration, new FakeHostEnvironment());

        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IFamilyContextResolver>();

        Assert.IsType<CoreFamilyContextResolver>(resolver);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Kin.KinHub.App.Functions";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
