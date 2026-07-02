using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Domain.KinListFeature;
using Microsoft.Extensions.Hosting;

namespace Kin.KinHub.KinList.Api.Common;

public sealed class IdempotencyRecordCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KinListOptions _options;
    private readonly ILogger<IdempotencyRecordCleanupService> _logger;

    public IdempotencyRecordCleanupService(
        IServiceScopeFactory scopeFactory,
        KinListOptions options,
        ILogger<IdempotencyRecordCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IdempotencyCleanupIntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupOnceAsync(stoppingToken);
        }
    }

    private async Task CleanupOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IIdempotencyRecordRepository>();
            var removedRecords = await repository.DeleteExpiredAsync(DateTime.UtcNow, cancellationToken);
            if (removedRecords > 0)
            {
                _logger.LogInformation("Removed {ExpiredRecordCount} expired KinList idempotency records.", removedRecords);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KinList idempotency cleanup failed.");
        }
    }
}
