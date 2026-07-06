namespace Kin.KinHub.Core.Business.Common;

public sealed class NoOpCoreTransactionExecutor : ICoreTransactionExecutor
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        operation(cancellationToken);
}
