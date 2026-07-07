namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface ICreateRecipeStepHandler
{
    Task<Result<RecipeStepResponse>> HandleAsync(
        CreateRecipeStepRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
