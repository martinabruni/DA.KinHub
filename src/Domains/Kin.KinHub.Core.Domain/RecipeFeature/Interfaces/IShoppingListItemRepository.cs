using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.RecipeFeature;

/// <summary>
/// Repository contract for <see cref="ShoppingListItem"/> entities.
/// </summary>
public interface IShoppingListItemRepository : IRepository<ShoppingListItem, Guid>
{
    /// <summary>
    /// Returns all items for the given shopping list, ordered by IsChecked ASC then CreatedAt ASC.
    /// </summary>
    Task<IReadOnlyList<ShoppingListItem>> GetAllByListIdAsync(Guid listId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new shopping list item and returns the created entity.
    /// </summary>
    Task<ShoppingListItem> AddAsync(ShoppingListItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts items that are not already present (case-insensitive dedup by Name within the list).
    /// Returns the count of items actually inserted.
    /// </summary>
    Task<int> AddBulkAsync(IEnumerable<ShoppingListItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the IsChecked flag of the item with the given id and returns the updated item.
    /// </summary>
    Task<ShoppingListItem> ToggleCheckedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes the item with the given id.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes all checked items belonging to the given shopping list.
    /// </summary>
    Task DeleteCheckedByListIdAsync(Guid listId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if an item with the given name already exists in the list (case-insensitive).
    /// </summary>
    Task<bool> ExistsByNameAsync(Guid listId, string name, CancellationToken cancellationToken = default);
}
