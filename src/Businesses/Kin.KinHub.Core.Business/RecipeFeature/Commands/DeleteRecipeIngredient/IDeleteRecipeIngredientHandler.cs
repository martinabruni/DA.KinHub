namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IDeleteRecipeIngredientHandler
{
    Task<Result<bool>> HandleAsync(
        Guid recipeIngredientId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
