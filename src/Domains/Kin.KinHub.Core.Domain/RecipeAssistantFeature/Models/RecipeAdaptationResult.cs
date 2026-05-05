using Kin.KinHub.Core.Domain.RecipeFeature;

namespace Kin.KinHub.Core.Domain.RecipeAssistantFeature;

public sealed record RecipeAdaptationResult
{
    public required Recipe OriginalRecipe { get; init; }
    public required Recipe AdaptedRecipe { get; init; }
    public required IReadOnlyList<RecipeChange> Changes { get; init; }
    public IReadOnlyList<Guid> ChangedOriginalIngredientIds { get; init; } = [];
}
