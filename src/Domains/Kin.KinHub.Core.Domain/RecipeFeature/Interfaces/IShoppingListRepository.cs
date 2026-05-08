using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.RecipeFeature;

/// <summary>
/// Repository contract for <see cref="ShoppingList"/> aggregates.
/// </summary>
public interface IShoppingListRepository : IRepository<ShoppingList, Guid>
{
    /// <summary>
    /// Returns all shopping lists belonging to the given family, ordered by UpdatedAt descending.
    /// </summary>
    Task<IReadOnlyList<ShoppingList>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the shopping list with the given id, or null if not found.
    /// </summary>
    Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new shopping list and returns the created entity.
    /// </summary>
    Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing shopping list and returns the updated entity.
    /// </summary>
    Task<ShoppingList> UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes the shopping list with the given id (cascade deletes items).
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
