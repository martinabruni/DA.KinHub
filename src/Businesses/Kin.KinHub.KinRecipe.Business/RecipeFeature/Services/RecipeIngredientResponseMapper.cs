using Mapster;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class RecipeIngredientResponseMapper : IRecipeIngredientResponseMapper
{
    public RecipeIngredientResponse Map(RecipeIngredient recipeIngredient) => recipeIngredient.Adapt<RecipeIngredientResponse>();
}
