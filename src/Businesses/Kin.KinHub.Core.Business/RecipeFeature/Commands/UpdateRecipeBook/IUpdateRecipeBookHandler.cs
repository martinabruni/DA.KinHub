namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IUpdateRecipeBookHandler
{
    Task<Result<RecipeBookResponse>> HandleAsync(
        Guid recipeBookId,
        UpdateRecipeBookRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
