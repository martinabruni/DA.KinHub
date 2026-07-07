using Kin.KinHub.KinRecipe.Domain.RecipeFeature;

namespace Kin.KinHub.KinRecipe.Domain.RecipeAssistantFeature;

public sealed record RecipeChange
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public Guid? OriginalIngredientId { get; init; }
    public RecipeIngredient? NewIngredient { get; init; }
}
