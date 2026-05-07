using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.RecipeAssistantFeature;

/// <summary>
/// Orchestrates AI-powered recipe features: suggestion from fridge contents, parsing from raw text, and constraint-based adaptation.
/// </summary>
public interface IRecipeAssistantManager
{
    /// <summary>Suggests recipes based on the fridge contents: existing family recipes with backend-computed match percentage, and up to 3 new AI-invented recipes.</summary>
    Task<Result<SuggestRecipesResult>> SuggestRecipesAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Parses a recipe from free text or unstructured input. Returns null in the result value if the text cannot be parsed.</summary>
    Task<Result<ParsedRecipeResponse?>> ParseRecipeAsync(
        string rawText,
        CancellationToken cancellationToken = default);

    /// <summary>Adapts a saved recipe according to the given constraints (e.g. "vegan", "no eggs", "serve 5").</summary>
    Task<Result<RecipeAdaptationResponse>> AdaptRecipeAsync(
        Guid recipeId,
        IReadOnlyList<string> constraints,
        Guid userId,
        CancellationToken cancellationToken = default);
}
