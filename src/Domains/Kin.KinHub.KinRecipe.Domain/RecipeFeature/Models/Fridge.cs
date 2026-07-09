using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.KinRecipe.Domain.RecipeFeature;

public sealed class Fridge : BaseDeletableEntity<Guid>
{
    public required string Name { get; set; }
    public required Guid FamilyId { get; set; }
}
