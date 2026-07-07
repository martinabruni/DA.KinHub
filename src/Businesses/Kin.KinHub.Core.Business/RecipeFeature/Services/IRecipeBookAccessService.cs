namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IRecipeBookAccessService
{
    Task<RecipeBookAccessResult> GetAccessibleRecipeBookAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
