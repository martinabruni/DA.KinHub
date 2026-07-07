using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.KinRecipe.Domain.RecipeFeature;

/// <summary>
/// Repository contract for <see cref="FridgeIngredient"/> aggregates.
/// </summary>
public interface IFridgeIngredientRepository
{
    /// <summary>Returns the fridge ingredient matching the given id, or null if not found.</summary>
    Task<FridgeIngredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all ingredients belonging to the given fridge.</summary>
    Task<IReadOnlyList<FridgeIngredient>> GetAllByFridgeIdAsync(Guid fridgeId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new fridge ingredient and returns it.</summary>
    Task<FridgeIngredient> AddAsync(FridgeIngredient ingredient, CancellationToken cancellationToken = default);

    /// <summary>Updates the given fridge ingredient and returns the updated state.</summary>
    Task<FridgeIngredient> UpdateAsync(FridgeIngredient ingredient, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the fridge ingredient with the given id.</summary>
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
