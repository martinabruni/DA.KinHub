using Kin.KinHub.Core.Domain.RecipeFeature;

namespace Kin.KinHub.Core.Domain.RecipeAssistantFeature;

/// <summary>
/// Service for AI-powered recipe assistance: suggestion, parsing, and adaptation.
/// </summary>
public interface IRecipeAssistantService
{
    /// <summary>Suggests up to 3 new AI-invented recipes based solely on the available fridge ingredients. Does not include family recipes.</summary>
    Task<IReadOnlyList<RecipeSuggestion>> SuggestNewRecipesAsync(
        IReadOnlyList<RecipeIngredient> fridgeIngredients,
        CancellationToken cancellationToken = default);

    /// <summary>Parses a recipe from free text or unstructured input. Returns null if the text cannot be parsed.</summary>
    Task<Recipe?> ParseRecipeAsync(
        string rawText,
        CancellationToken cancellationToken = default);

    /// <summary>Adapts a recipe according to the given constraints (e.g. "vegan", "no eggs", "serve 5").</summary>
    Task<RecipeAdaptationResult> AdaptRecipeAsync(
        Recipe recipe,
        IReadOnlyList<string> constraints,
        CancellationToken cancellationToken = default);
}
