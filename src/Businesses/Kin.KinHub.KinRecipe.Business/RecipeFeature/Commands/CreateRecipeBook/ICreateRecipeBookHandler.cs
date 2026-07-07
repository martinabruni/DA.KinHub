namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface ICreateRecipeBookHandler
{
    Task<Result<RecipeBookResponse>> HandleAsync(
        CreateRecipeBookRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
