using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IFridgeIngredientService
{
    Task<Result<FridgeIngredientResponse>> CreateAsync(CreateFridgeIngredientRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<FridgeIngredientResponse>>> GetAllAsync(Guid fridgeId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<FridgeIngredientResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<FridgeIngredientResponse>> UpdateAsync(Guid id, UpdateFridgeIngredientRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
