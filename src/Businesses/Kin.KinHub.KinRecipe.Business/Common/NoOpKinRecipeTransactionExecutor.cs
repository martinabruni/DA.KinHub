namespace Kin.KinHub.KinRecipe.Business.Common;

public sealed class NoOpKinRecipeTransactionExecutor : IKinRecipeTransactionExecutor
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        operation(cancellationToken);
}
