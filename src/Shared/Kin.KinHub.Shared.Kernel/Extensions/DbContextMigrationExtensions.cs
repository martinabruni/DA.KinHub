namespace Kin.KinHub.Shared.Kernel.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public static class DbContextMigrationExtensions
{
    public static async Task ApplyPendingMigrationsAsync<TContext>(
        this TContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        var contextName = typeof(TContext).Name;
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogInformation("[migrations] Applying {Context} migrations...", contextName);

        try
        {
            await context.Database.MigrateAsync(cancellationToken);
            var elapsed = DateTimeOffset.UtcNow - startedAt;
            logger.LogInformation("[migrations] {Context} migrations applied in {ElapsedSeconds:F1}s.", contextName, elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            var elapsed = DateTimeOffset.UtcNow - startedAt;
            logger.LogError(ex, "[migrations] {Context} migrations failed after {ElapsedSeconds:F1}s.", contextName, elapsed.TotalSeconds);
            throw;
        }
    }
}
