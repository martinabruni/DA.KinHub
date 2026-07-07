using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.KinRecipe.Domain.RecipeFeature;

public sealed class RecipeIngredient : BaseEmbeddingEntity<Guid>
{
    public required string Name { get; set; }
    public required string MeasureUnit { get; set; }
    public required decimal Quantity { get; set; }
    public required Guid RecipeId { get; set; }
}
