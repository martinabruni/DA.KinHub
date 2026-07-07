namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface ICreateRecipeHandler
{
    Task<Result<RecipeResponse>> HandleAsync(
        CreateRecipeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
