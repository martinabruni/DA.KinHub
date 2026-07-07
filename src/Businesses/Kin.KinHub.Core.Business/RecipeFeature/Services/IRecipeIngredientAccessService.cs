namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IRecipeIngredientAccessService
{
    Task<RecipeIngredientAccessResult> GetAccessibleRecipeIngredientAsync(
        Guid recipeIngredientId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
