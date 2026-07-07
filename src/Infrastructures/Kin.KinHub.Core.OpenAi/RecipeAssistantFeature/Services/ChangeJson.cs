namespace Kin.KinHub.Core.OpenAi.RecipeAssistantFeature;

internal sealed record ChangeJson(
    string Type,
    string Description,
    string? OriginalIngredientId,
    IngredientJson? NewIngredient);
