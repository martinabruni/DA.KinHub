namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class CreateFridgeIngredientRequest
{
    public required string Name { get; init; }
    public required string MeasureUnit { get; init; }
    public required decimal Quantity { get; init; }
    public required Guid FridgeId { get; init; }
}
