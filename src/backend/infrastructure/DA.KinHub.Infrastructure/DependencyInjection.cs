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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DA.KinHub.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringMode = "ConnectionString";
    private const string ManagedIdentityMode = "ManagedIdentity";
    private const string AzurePostgreSqlScope = "https://ossrdbms-aad.database.windows.net/.default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName)).ValidateOnStart();
        services.AddOptions<BlobStorageOptions>().Bind(configuration.GetSection(BlobStorageOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();
        services.AddSingleton<IValidateOptions<BlobStorageOptions>, BlobStorageOptionsValidator>();

        services.TryAddSingleton<TokenCredential>(provider =>
        {
            var environment = provider.GetService<IHostEnvironment>();
            return environment?.IsDevelopment() == true
                ? new DefaultAzureCredential()
                : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
        });

        services.AddSingleton(sp => CreateDataSource(
            sp.GetRequiredService<IOptions<DatabaseOptions>>().Value,
            sp.GetRequiredService<TokenCredential>()));
        services.AddSingleton<IDocumentStorage, BlobDocumentStorage>();
        services.AddDbContext<KinHubDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource, npgsql =>
            {
                npgsql.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                npgsql.MigrationsAssembly(typeof(KinHubDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
            });
        });
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IFamilyMembershipRepository, FamilyMembershipRepository>();
        services.AddHealthChecks().AddDbContextCheck<KinHubDbContext>("postgresql", tags: [InfrastructureHealthChecks.ReadyTag]);
        services.AddHostedService<DatabaseMigrationHostedService>();
        return services;
    }

    private static NpgsqlDataSource CreateDataSource(DatabaseOptions options, TokenCredential credential)
    {
        var builder = new NpgsqlConnectionStringBuilder();

        switch (options.Mode)
        {
            case ConnectionStringMode:
                builder.ConnectionString = options.ConnectionString;
                break;
            case ManagedIdentityMode:
                builder.Host = options.Host;
                builder.Port = options.Port;
                builder.Database = options.DatabaseName;
                builder.Username = options.Username;
                builder.SslMode = options.RequireSsl ? SslMode.Require : SslMode.Prefer;
                break;
            default:
                throw new InvalidOperationException($"Unsupported database mode '{options.Mode}'.");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.ConnectionString);

        if (string.Equals(options.Mode, ManagedIdentityMode, StringComparison.Ordinal))
        {
            dataSourceBuilder.UsePeriodicPasswordProvider(
                async (_, cancellationToken) =>
                {
                    var accessToken = await credential.GetTokenAsync(new TokenRequestContext([AzurePostgreSqlScope]), cancellationToken);
                    return accessToken.Token;
                },
                successRefreshInterval: TimeSpan.FromMinutes(55),
                failureRefreshInterval: TimeSpan.FromMinutes(5));
        }

        return dataSourceBuilder.Build();
    }
}
