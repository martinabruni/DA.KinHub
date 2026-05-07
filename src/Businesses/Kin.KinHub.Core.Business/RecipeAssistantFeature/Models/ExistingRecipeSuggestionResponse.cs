namespace Kin.KinHub.Core.Business.RecipeAssistantFeature;

public sealed class ExistingRecipeSuggestionResponse
{
    public required Guid RecipeId { get; init; }
    public required string Name { get; init; }
    public required int MatchPercentage { get; init; }
    public IReadOnlyList<AssistantIngredientResponse> MissingIngredients { get; init; } = [];
}
