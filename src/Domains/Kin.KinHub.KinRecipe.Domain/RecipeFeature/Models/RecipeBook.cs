using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.KinRecipe.Domain.RecipeFeature;

public sealed class RecipeBook : BaseDeletableEntity<Guid>
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required Guid FamilyId { get; set; }
}
