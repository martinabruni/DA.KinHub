namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IDeleteRecipeHandler
{
    Task<Result<bool>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
