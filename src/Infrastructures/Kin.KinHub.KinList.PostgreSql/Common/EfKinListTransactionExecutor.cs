using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.PostgreSql.Models;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.KinList.PostgreSql.Common;

internal sealed class EfKinListTransactionExecutor : IKinListTransactionExecutor
{
    private readonly KinListDbContext _context;

    public EfKinListTransactionExecutor(KinListDbContext context)
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
