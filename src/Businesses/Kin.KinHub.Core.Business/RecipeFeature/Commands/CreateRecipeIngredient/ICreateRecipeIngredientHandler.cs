namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface ICreateRecipeIngredientHandler
{
    Task<Result<RecipeIngredientResponse>> HandleAsync(
        CreateRecipeIngredientRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
