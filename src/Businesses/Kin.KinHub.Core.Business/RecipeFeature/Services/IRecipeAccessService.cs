namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IRecipeAccessService
{
    Task<RecipeAccessResult> GetAccessibleRecipeAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
