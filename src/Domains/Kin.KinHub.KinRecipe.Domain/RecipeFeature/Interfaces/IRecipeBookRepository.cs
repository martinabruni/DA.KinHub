using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.KinRecipe.Domain.RecipeFeature;

/// <summary>
/// Repository contract for <see cref="RecipeBook"/> aggregates.
/// </summary>
public interface IRecipeBookRepository
{
    /// <summary>Returns the recipe book matching the given id, or null if not found.</summary>
    Task<RecipeBook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all recipe books belonging to the given family.</summary>
    Task<IReadOnlyList<RecipeBook>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new recipe book and returns it.</summary>
    Task<RecipeBook> AddAsync(RecipeBook recipeBook, CancellationToken cancellationToken = default);

    /// <summary>Updates the given recipe book and returns the updated state.</summary>
    Task<RecipeBook> UpdateAsync(RecipeBook recipeBook, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the recipe book with the given id.</summary>
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
