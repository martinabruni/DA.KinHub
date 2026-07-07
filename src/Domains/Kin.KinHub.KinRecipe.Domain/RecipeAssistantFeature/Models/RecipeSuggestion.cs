using Kin.KinHub.KinRecipe.Domain.RecipeFeature;

namespace Kin.KinHub.KinRecipe.Domain.RecipeAssistantFeature;

public sealed record RecipeSuggestion
{
    public required Recipe Recipe { get; init; }
    public required int MatchPercentage { get; init; }
    public required IReadOnlyList<RecipeIngredient> MissingIngredients { get; init; }
}
