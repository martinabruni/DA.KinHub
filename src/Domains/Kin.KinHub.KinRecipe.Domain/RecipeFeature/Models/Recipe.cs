using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.KinRecipe.Domain.RecipeFeature;

public sealed class Recipe : BaseDeletableEntity<Guid>
{
    public required string Name { get; set; }
    public string? Backstory { get; set; }
    public required TimeSpan FinalTime { get; set; }
    public required int Portions { get; set; }
    public required Guid RecipeBookId { get; set; }
    public IReadOnlyList<RecipeIngredient>? Ingredients { get; set; }
    public IReadOnlyList<RecipeStep>? Steps { get; set; }
}
