namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IUpdateRecipeStepHandler
{
    Task<Result<RecipeStepResponse>> HandleAsync(
        Guid recipeStepId,
        UpdateRecipeStepRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
