namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IGetRecipeIngredientsHandler
{
    Task<Result<IReadOnlyList<RecipeIngredientResponse>>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
