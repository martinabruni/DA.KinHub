namespace Kin.KinHub.Core.OpenAi.RecipeAssistantFeature;

internal sealed record ParseResponse(
    string TaskType,
    RecipeJson? Recipe,
    string? Error);
