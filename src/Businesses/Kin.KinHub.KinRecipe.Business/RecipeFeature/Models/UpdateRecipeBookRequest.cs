namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class UpdateRecipeBookRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
