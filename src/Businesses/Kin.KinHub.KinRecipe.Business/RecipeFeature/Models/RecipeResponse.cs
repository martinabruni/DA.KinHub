namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class RecipeResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Backstory { get; init; }
    public required TimeSpan FinalTime { get; init; }
    public required int Portions { get; init; }
    public required Guid RecipeBookId { get; init; }
    public IReadOnlyList<RecipeIngredientResponse> Ingredients { get; init; } = [];
    public IReadOnlyList<RecipeStepResponse> Steps { get; init; } = [];
}
