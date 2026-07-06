using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.RecipeFeature;

/// <summary>
/// Repository contract for <see cref="RecipeIngredient"/> aggregates.
/// </summary>
public interface IRecipeIngredientRepository
{
    /// <summary>Returns the recipe ingredient matching the given id, or null if not found.</summary>
    Task<RecipeIngredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all ingredients belonging to the given recipe.</summary>
    Task<IReadOnlyList<RecipeIngredient>> GetAllByRecipeIdAsync(Guid recipeId, CancellationToken cancellationToken = default);

    /// <summary>Returns all ingredients belonging to the given recipes.</summary>
    Task<IReadOnlyList<RecipeIngredient>> GetAllByRecipeIdsAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken = default);

    /// <summary>Persists a new recipe ingredient and returns it.</summary>
    Task<RecipeIngredient> AddAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default);

    /// <summary>Persists a set of new recipe ingredients and returns them in the same order.</summary>
    Task<IReadOnlyList<RecipeIngredient>> AddRangeAsync(IReadOnlyCollection<RecipeIngredient> ingredients, CancellationToken cancellationToken = default);

    /// <summary>Updates the given recipe ingredient and returns the updated state.</summary>
    Task<RecipeIngredient> UpdateAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the recipe ingredient with the given id.</summary>
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
