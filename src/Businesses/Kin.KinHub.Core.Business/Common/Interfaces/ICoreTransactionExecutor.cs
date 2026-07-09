namespace Kin.KinHub.Core.Business.Common;

public interface ICoreTransactionExecutor
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
