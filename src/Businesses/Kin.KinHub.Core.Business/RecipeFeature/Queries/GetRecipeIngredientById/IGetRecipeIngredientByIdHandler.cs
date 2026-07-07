namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipeIngredientByIdHandler
{
    Task<Result<RecipeIngredientResponse>> HandleAsync(
        Guid recipeIngredientId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
