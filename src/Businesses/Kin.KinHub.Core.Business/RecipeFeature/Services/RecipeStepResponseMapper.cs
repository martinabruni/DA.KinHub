using Mapster;

namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class RecipeStepResponseMapper : IRecipeStepResponseMapper
{
    public RecipeStepResponse Map(RecipeStep recipeStep) => recipeStep.Adapt<RecipeStepResponse>();
}
