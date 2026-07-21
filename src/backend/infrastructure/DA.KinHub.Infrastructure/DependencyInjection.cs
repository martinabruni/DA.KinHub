using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Documents;
using DA.KinHub.Domain.Identity;
using DA.KinHub.Infrastructure.Persistence;
using DA.KinHub.Infrastructure.Storage;
using Azure.Core;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DA.KinHub.Infrastructure;

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
        services.AddHealthChecks().AddDbContextCheck<KinHubDbContext>("postgresql", tags: ["ready"]);
        services.AddHostedService<DatabaseMigrationHostedService>();
        return services;
    }
}
