namespace Kin.KinHub.KinRecipe.AzureOpenAi.RecipeAssistantFeature;

internal sealed record SuggestionResponse(
    string TaskType,
    IReadOnlyList<SuggestionItem> Suggestions);
