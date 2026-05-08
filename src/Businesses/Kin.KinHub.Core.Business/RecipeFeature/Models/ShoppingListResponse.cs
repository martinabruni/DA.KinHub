namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class ShoppingListResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Guid FamilyId { get; set; }
    public int ItemCount { get; set; }
    public int CheckedCount { get; set; }
}
