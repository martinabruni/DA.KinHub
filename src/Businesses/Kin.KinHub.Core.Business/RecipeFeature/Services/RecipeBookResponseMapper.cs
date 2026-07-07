using Mapster;

namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class RecipeBookResponseMapper : IRecipeBookResponseMapper
{
    public RecipeBookResponse Map(RecipeBook recipeBook) => recipeBook.Adapt<RecipeBookResponse>();
}
