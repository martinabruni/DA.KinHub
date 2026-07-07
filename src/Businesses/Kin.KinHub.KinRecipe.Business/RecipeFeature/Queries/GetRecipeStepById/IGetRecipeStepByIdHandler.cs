namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IGetRecipeStepByIdHandler
{
    Task<Result<RecipeStepResponse>> HandleAsync(
        Guid recipeStepId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
