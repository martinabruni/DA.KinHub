using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IRecipeService
{
    Task<Result<RecipeResponse>> CreateAsync(CreateRecipeRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecipeResponse>>> GetAllAsync(Guid recipeBookId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RecipeResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RecipeResponse>> UpdateAsync(Guid id, UpdateRecipeRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
