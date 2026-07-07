namespace Kin.KinHub.KinRecipe.Business.RecipeAssistantFeature;

public sealed class RecipeChangeResponse
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public Guid? OriginalIngredientId { get; init; }
}
