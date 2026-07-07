namespace Kin.KinHub.Core.OpenAi.RecipeAssistantFeature;

internal sealed record SuggestionResponse(
    string TaskType,
    IReadOnlyList<SuggestionItem> Suggestions);
