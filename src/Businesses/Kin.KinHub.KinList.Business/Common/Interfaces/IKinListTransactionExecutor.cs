namespace Kin.KinHub.KinList.Business.Common;

public interface IKinListTransactionExecutor
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
