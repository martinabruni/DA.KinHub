using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.RecipeFeature;

public sealed class ShoppingList : BaseEntity<Guid>
{
    public required string Name { get; set; }
    public required Guid FamilyId { get; set; }
}
