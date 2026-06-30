namespace Kin.KinHub.KinList.Business.Common;

internal sealed class NoOpKinListTransactionExecutor : IKinListTransactionExecutor
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        operation(cancellationToken);
}
