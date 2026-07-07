using Kin.KinHub.Core.Business.Common;
using Kin.KinHub.Core.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.Common;

internal sealed class EfCoreTransactionExecutor : ICoreTransactionExecutor
{
    private readonly CoreDbContext _context;

    public EfCoreTransactionExecutor(CoreDbContext context)
    {
        _context = context;
    }

    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
