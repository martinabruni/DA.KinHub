namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IRecipeStepAccessService
{
    Task<RecipeStepAccessResult> GetAccessibleRecipeStepAsync(
        Guid recipeStepId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
