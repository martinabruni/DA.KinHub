namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IRecipeResponseMapper
{
    Task<RecipeResponse> MapAsync(
        Recipe recipe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps a set of recipes, loading their ingredients and steps with a single batch query each
    /// (avoids the N+1 reads produced by mapping recipes one by one).
    /// </summary>
    Task<IReadOnlyList<RecipeResponse>> MapAsync(
        IReadOnlyList<Recipe> recipes,
        CancellationToken cancellationToken = default);
}
