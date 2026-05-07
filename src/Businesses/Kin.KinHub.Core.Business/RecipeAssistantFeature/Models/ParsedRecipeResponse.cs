namespace Kin.KinHub.Core.Business.RecipeAssistantFeature;

public sealed class ParsedRecipeResponse
{
    public required string Name { get; init; }
    public string? Backstory { get; init; }
    public required TimeSpan FinalTime { get; init; }
    public required int Portions { get; init; }
    public IReadOnlyList<AssistantIngredientResponse> Ingredients { get; init; } = [];
    public IReadOnlyList<AssistantStepResponse> Steps { get; init; } = [];
}
