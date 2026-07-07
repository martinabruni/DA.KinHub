using Kin.KinHub.Identity.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubIdentityPostgreSqlInfrastructure(
        this IServiceCollection services,
        Action<PostgreSqlOptions> configure)
    {
        var options = new PostgreSqlOptions();
        configure(options);
        options.Validate();

        services.AddDbContext<IdentityDbContext>(o =>
            o.UseNpgsql(options.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IKinUserRepository, KinUserRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IUserProviderRepository, UserProviderRepository>();
        services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }
}
