namespace Kin.KinHub.Core.OpenAi.RecipeAssistantFeature;

internal sealed record SuggestionItem(
    RecipeJson Recipe,
    int MatchPercentage,
    IReadOnlyList<IngredientJson> MissingIngredients);
