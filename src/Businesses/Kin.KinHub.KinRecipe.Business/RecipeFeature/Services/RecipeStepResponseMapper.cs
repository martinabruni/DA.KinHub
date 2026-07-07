using Mapster;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class RecipeStepResponseMapper : IRecipeStepResponseMapper
{
    public RecipeStepResponse Map(RecipeStep recipeStep) => recipeStep.Adapt<RecipeStepResponse>();
}
