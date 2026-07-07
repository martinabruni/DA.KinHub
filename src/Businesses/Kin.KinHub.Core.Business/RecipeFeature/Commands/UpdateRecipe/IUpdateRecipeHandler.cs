namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IUpdateRecipeHandler
{
    Task<Result<RecipeResponse>> HandleAsync(
        Guid recipeId,
        UpdateRecipeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
