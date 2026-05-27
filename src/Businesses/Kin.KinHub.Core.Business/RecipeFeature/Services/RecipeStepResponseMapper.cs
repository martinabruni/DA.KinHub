namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IRecipeStepResponseMapper
{
    RecipeStepResponse Map(RecipeStep recipeStep);
}

public sealed class RecipeStepResponseMapper : IRecipeStepResponseMapper
{
    public RecipeStepResponse Map(RecipeStep recipeStep) =>
        new()
        {
            Id = recipeStep.Id,
            Order = recipeStep.Order,
            Description = recipeStep.Description,
            RecipeId = recipeStep.RecipeId,
        };
}
