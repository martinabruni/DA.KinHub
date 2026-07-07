namespace Kin.KinHub.KinRecipe.Business.RecipeAssistantFeature;

public sealed class SuggestRecipesResult
{
    public IReadOnlyList<ExistingRecipeSuggestionResponse> ExistingRecipes { get; init; } = [];
    public IReadOnlyList<RecipeSuggestionResponse> NewRecipes { get; init; } = [];
}
