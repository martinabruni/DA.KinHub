using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvancedFrontier.Infrastructure.Persistence;

internal sealed class DatabaseMigrationHostedService(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseMigrationHostedService> logger) : IHostedService
{
    private const long AdvisoryLockId = 4_842_664_982;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup)
        {
            return;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Startup migrations are allowed only in Development. Use the CI migration bundle in other environments.");
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KinHubDbContext>();
        logger.LogInformation("Acquiring PostgreSQL advisory lock before local migrations");
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_lock({AdvisoryLockId});", cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({AdvisoryLockId});", CancellationToken.None);
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
