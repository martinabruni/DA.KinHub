namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class BulkAddShoppingListItemsRequest
{
    public required IReadOnlyList<string> Names { get; set; }
    public required Guid ShoppingListId { get; set; }
}
