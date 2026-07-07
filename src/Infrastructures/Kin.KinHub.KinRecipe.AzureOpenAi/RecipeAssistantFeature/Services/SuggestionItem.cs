namespace Kin.KinHub.KinRecipe.AzureOpenAi.RecipeAssistantFeature;

internal sealed record SuggestionItem(
    RecipeJson Recipe,
    int MatchPercentage,
    IReadOnlyList<IngredientJson> MissingIngredients);
