namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IDeleteRecipeBookHandler
{
    Task<Result<bool>> HandleAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
