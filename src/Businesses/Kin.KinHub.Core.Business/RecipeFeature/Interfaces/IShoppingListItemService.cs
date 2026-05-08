using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.RecipeFeature;

/// <summary>
/// Business service for shopping list item management.
/// </summary>
public interface IShoppingListItemService
{
    /// <summary>
    /// Returns all items for the given shopping list (ownership validated).
    /// </summary>
    Task<Result<IReadOnlyList<ShoppingListItemResponse>>> GetAllByListIdAsync(
        Guid listId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a single item to the given shopping list (ownership validated).
    /// </summary>
    Task<Result<ShoppingListItemResponse>> AddAsync(
        Guid listId,
        CreateShoppingListItemRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-adds items to the given shopping list, deduplicating by name (case-insensitive).
    /// Returns the count of items actually inserted.
    /// </summary>
    Task<Result<BulkAddShoppingListItemsResponse>> BulkAddAsync(
        Guid listId,
        BulkAddShoppingListItemsRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the IsChecked flag of the given item (ownership validated).
    /// </summary>
    Task<Result<ShoppingListItemResponse>> ToggleCheckedAsync(
        Guid listId,
        Guid itemId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes a single item (ownership validated).
    /// </summary>
    Task<Result<bool>> DeleteAsync(
        Guid listId,
        Guid itemId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes all checked items from the given shopping list (ownership validated).
    /// </summary>
    Task<Result<bool>> DeleteCheckedAsync(
        Guid listId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
