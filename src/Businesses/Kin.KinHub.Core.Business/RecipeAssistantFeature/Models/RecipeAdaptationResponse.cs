namespace Kin.KinHub.Core.Business.RecipeAssistantFeature;

public sealed class RecipeAdaptationResponse
{
    public required ParsedRecipeResponse OriginalRecipe { get; init; }
    public required ParsedRecipeResponse AdaptedRecipe { get; init; }
    public IReadOnlyList<RecipeChangeResponse> Changes { get; init; } = [];
    public IReadOnlyList<Guid> ChangedOriginalIngredientIds { get; init; } = [];
}
