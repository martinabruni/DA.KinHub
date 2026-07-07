namespace Kin.KinHub.KinRecipe.Business.RecipeAssistantFeature;

public sealed class AdaptRecipeRequest
{
    public required Guid RecipeId { get; init; }
    public required IReadOnlyList<string> Constraints { get; init; }
}
