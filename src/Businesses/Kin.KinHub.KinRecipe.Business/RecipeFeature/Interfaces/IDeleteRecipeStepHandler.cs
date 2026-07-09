namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IDeleteRecipeStepHandler
{
    Task<Result<bool>> HandleAsync(
        Guid recipeStepId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
