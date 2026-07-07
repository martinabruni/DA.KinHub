using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public interface IRecipeBookService
{
    Task<Result<RecipeBookResponse>> CreateAsync(CreateRecipeBookRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecipeBookResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RecipeBookResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RecipeBookResponse>> UpdateAsync(Guid id, UpdateRecipeBookRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
