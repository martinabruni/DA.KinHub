using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.KinRecipe.Domain.RecipeFeature;

/// <summary>
/// Repository contract for <see cref="Fridge"/> aggregates.
/// </summary>
public interface IFridgeRepository
{
    /// <summary>Returns the fridge matching the given id, or null if not found.</summary>
    Task<Fridge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all fridges belonging to the given family.</summary>
    Task<IReadOnlyList<Fridge>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new fridge and returns it.</summary>
    Task<Fridge> AddAsync(Fridge fridge, CancellationToken cancellationToken = default);

    /// <summary>Updates the given fridge and returns the updated state.</summary>
    Task<Fridge> UpdateAsync(Fridge fridge, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the fridge with the given id.</summary>
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
