using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IFridgeService
{
    Task<Result<FridgeResponse>> CreateAsync(CreateFridgeRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<FridgeResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<FridgeResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<FridgeResponse>> UpdateAsync(Guid id, UpdateFridgeRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
