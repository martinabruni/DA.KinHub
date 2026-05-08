namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class ShoppingListItemResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsChecked { get; set; }
    public DateTime CreatedAt { get; set; }
}
