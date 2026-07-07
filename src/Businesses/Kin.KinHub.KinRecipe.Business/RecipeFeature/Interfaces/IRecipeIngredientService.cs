using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IRecipeIngredientService
{
    Task<Result<RecipeIngredientResponse>> CreateAsync(CreateRecipeIngredientRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecipeIngredientResponse>>> GetAllAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RecipeIngredientResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RecipeIngredientResponse>> UpdateAsync(Guid id, UpdateRecipeIngredientRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
