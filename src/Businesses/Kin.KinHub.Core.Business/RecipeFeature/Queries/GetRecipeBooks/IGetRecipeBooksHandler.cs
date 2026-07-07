namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipeBooksHandler
{
    Task<Result<IReadOnlyList<RecipeBookResponse>>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
