namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IGetRecipeBooksHandler
{
    Task<Result<IReadOnlyList<RecipeBookResponse>>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
