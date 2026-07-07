namespace Kin.KinHub.KinRecipe.AzureOpenAi.RecipeAssistantFeature;

internal sealed record ParseResponse(
    string TaskType,
    RecipeJson? Recipe,
    string? Error);
