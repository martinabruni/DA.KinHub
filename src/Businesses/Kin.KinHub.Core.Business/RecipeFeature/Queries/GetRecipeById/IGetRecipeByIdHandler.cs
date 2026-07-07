namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipeByIdHandler
{
    Task<Result<RecipeResponse>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
