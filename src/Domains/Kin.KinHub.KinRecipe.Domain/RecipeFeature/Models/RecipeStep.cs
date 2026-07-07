using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.KinRecipe.Domain.RecipeFeature;

public sealed class RecipeStep : BaseDeletableEntity<Guid>
{
    public required int Order { get; set; }
    public required string Description { get; set; }
    public required Guid RecipeId { get; set; }
}
