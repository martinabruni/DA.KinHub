namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IUpdateRecipeIngredientHandler
{
    Task<Result<RecipeIngredientResponse>> HandleAsync(
        Guid recipeIngredientId,
        UpdateRecipeIngredientRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
