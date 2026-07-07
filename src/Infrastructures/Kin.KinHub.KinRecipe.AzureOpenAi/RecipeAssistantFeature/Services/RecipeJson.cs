namespace Kin.KinHub.KinRecipe.AzureOpenAi.RecipeAssistantFeature;

internal sealed record RecipeJson(
    string Name,
    string? Backstory,
    string FinalTime,
    int Portions,
    IReadOnlyList<IngredientJson> Ingredients,
    IReadOnlyList<StepJson> Steps);
