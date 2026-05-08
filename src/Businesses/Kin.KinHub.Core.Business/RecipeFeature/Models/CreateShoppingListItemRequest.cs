namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class CreateShoppingListItemRequest
{
    public required string Name { get; set; }
    public required Guid ShoppingListId { get; set; }
}
