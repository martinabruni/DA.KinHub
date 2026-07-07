using Mapster;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class RecipeBookResponseMapper : IRecipeBookResponseMapper
{
    public RecipeBookResponse Map(RecipeBook recipeBook) => recipeBook.Adapt<RecipeBookResponse>();
}
