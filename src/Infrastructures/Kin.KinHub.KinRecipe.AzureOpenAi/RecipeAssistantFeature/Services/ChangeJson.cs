namespace Kin.KinHub.KinRecipe.AzureOpenAi.RecipeAssistantFeature;

internal sealed record ChangeJson(
    string Type,
    string Description,
    string? OriginalIngredientId,
    IngredientJson? NewIngredient);
