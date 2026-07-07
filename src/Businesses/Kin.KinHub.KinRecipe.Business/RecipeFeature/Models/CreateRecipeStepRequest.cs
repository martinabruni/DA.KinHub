namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class CreateRecipeStepRequest
{
    public required int Order { get; init; }
    public required string Description { get; init; }
    public required Guid RecipeId { get; init; }
}
