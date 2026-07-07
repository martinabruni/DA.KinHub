namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipeBookByIdHandler
{
    Task<Result<RecipeBookResponse>> HandleAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
