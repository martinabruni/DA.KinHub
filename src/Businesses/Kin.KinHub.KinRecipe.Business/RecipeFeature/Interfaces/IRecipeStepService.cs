using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IRecipeStepService
{
    Task<Result<RecipeStepResponse>> CreateAsync(CreateRecipeStepRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecipeStepResponse>>> GetAllAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RecipeStepResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RecipeStepResponse>> UpdateAsync(Guid id, UpdateRecipeStepRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
