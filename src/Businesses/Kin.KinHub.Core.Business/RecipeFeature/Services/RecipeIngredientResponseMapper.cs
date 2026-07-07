using Mapster;

namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class RecipeIngredientResponseMapper : IRecipeIngredientResponseMapper
{
    public RecipeIngredientResponse Map(RecipeIngredient recipeIngredient) => recipeIngredient.Adapt<RecipeIngredientResponse>();
}
