namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IGetRecipesHandler
{
    Task<Result<IReadOnlyList<RecipeResponse>>> HandleAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
