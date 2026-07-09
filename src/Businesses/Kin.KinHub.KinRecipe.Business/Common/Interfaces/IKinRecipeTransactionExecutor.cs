namespace Kin.KinHub.KinRecipe.Business.Common;

public interface IKinRecipeTransactionExecutor
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
