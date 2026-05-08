using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.RecipeFeature;

public sealed class ShoppingListItem : BaseEntity<Guid>
{
    public required string Name { get; set; }
    public bool IsChecked { get; set; }
    public required Guid ShoppingListId { get; set; }
}
