namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipesHandler
{
    Task<Result<IReadOnlyList<RecipeResponse>>> HandleAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
