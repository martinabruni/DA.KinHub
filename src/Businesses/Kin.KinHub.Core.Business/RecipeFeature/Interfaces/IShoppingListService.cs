using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.RecipeFeature;

/// <summary>
/// Business service for shopping list management.
/// </summary>
public interface IShoppingListService
{
    /// <summary>
    /// Returns all shopping lists for the family of the given user.
    /// </summary>
    Task<Result<IReadOnlyList<ShoppingListResponse>>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single shopping list by id (ownership validated).
    /// </summary>
    Task<Result<ShoppingListResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new shopping list for the family of the given user.
    /// </summary>
    Task<Result<ShoppingListResponse>> CreateAsync(
        CreateShoppingListRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames an existing shopping list (ownership validated).
    /// </summary>
    Task<Result<ShoppingListResponse>> UpdateAsync(
        Guid id,
        UpdateShoppingListRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes a shopping list and all its items (ownership validated).
    /// </summary>
    Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}
