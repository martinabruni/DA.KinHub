using AdvancedFrontier.Domain.Families;
using AdvancedFrontier.Domain.Documents;
using AdvancedFrontier.Domain.Identity;
using AdvancedFrontier.Domain.Projects;
using AdvancedFrontier.Infrastructure.Persistence;
using AdvancedFrontier.Infrastructure.Storage;
using Azure.Core;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AdvancedFrontier.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required.");
        var timeout = configuration.GetValue<int?>("Database:CommandTimeoutSeconds") ?? 30;

        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddOptions<BlobStorageOptions>().Bind(configuration.GetSection(BlobStorageOptions.SectionName));
        services.AddSingleton<TokenCredential>(provider =>
        {
            var environment = provider.GetService<IHostEnvironment>();
            return environment?.IsDevelopment() == true
                ? new DefaultAzureCredential()
                : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
        });
        services.AddSingleton<IDocumentStorage, BlobDocumentStorage>();
        services.AddDbContext<KinHubDbContext>(options => options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.CommandTimeout(timeout);
            npgsql.MigrationsAssembly(typeof(KinHubDbContext).Assembly.FullName);
            npgsql.EnableRetryOnFailure(3);
        }));
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<IFamilyMembershipRepository, FamilyMembershipRepository>();
        services.AddScoped<IFamilyProjectRepository, FamilyProjectRepository>();
        services.AddHealthChecks().AddDbContextCheck<KinHubDbContext>("postgresql", tags: ["ready"]);
        services.AddHostedService<DatabaseMigrationHostedService>();
        return services;
    }
}
