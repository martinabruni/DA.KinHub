namespace Kin.KinHub.KinRecipe.Business.RecipeAssistantFeature;

public sealed class AssistantIngredientResponse
{
    public Guid? Id { get; init; }
    public required string Name { get; init; }
    public required decimal Quantity { get; init; }
    public required string MeasureUnit { get; init; }
}
