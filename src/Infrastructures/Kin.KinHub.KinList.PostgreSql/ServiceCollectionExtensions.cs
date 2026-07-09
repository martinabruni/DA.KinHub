using Kin.KinHub.KinList.PostgreSql.Common;
using Kin.KinHub.KinList.PostgreSql.KinListFeature;
using Kin.KinHub.KinList.PostgreSql;
using Kin.KinHub.KinList.Domain.KinListFeature;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.Shared.Kernel.Options;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubKinListPostgreSqlInfrastructure(
        this IServiceCollection services,
        Action<PostgreSqlOptions> configure)
    {
        var options = new PostgreSqlOptions();
        configure(options);
        options.Validate();

        services.AddDbContext<KinListDbContext>(o =>
            o.UseNpgsql(options.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        services.AddScoped<IKinListRepository, KinListRepository>();
        services.AddScoped<IKinListItemRepository, KinListItemRepository>();
        services.AddScoped<IIdempotencyRecordRepository, IdempotencyRecordRepository>();
        services.AddScoped<IAudioProcessingOperationRepository, AudioProcessingOperationRepository>();
        services.AddScoped<IKinListTransactionExecutor, EfKinListTransactionExecutor>();

        return services;
    }
}
