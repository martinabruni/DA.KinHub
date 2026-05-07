namespace Kin.KinHub.Core.Business.RecipeAssistantFeature;

public sealed class RecipeSuggestionResponse
{
    public required ParsedRecipeResponse Recipe { get; init; }
    public required int MatchPercentage { get; init; }
    public IReadOnlyList<AssistantIngredientResponse> MissingIngredients { get; init; } = [];
}
