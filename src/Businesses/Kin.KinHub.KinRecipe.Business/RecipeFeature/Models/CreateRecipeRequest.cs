namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class CreateRecipeRequest
{
    public required string Name { get; init; }
    public string? Backstory { get; init; }
    public required TimeSpan FinalTime { get; init; }
    public required int Portions { get; init; }
    public required Guid RecipeBookId { get; init; }
    public IReadOnlyList<CreateRecipeIngredientInlineRequest>? Ingredients { get; init; }
    public IReadOnlyList<CreateRecipeStepInlineRequest>? Steps { get; init; }
}
