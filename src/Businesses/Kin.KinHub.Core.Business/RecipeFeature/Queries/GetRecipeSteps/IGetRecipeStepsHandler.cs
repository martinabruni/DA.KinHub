namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipeStepsHandler
{
    Task<Result<IReadOnlyList<RecipeStepResponse>>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
