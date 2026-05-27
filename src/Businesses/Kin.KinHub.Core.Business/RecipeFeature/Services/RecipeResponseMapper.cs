namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IRecipeResponseMapper
{
    Task<RecipeResponse> MapAsync(
        Recipe recipe,
        CancellationToken cancellationToken = default);
}

public sealed class RecipeResponseMapper : IRecipeResponseMapper
{
    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IRecipeStepRepository _recipeStepRepository;

    public RecipeResponseMapper(
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeStepRepository recipeStepRepository)
    {
        _recipeIngredientRepository = recipeIngredientRepository;
        _recipeStepRepository = recipeStepRepository;
    }

    public async Task<RecipeResponse> MapAsync(
        Recipe recipe,
        CancellationToken cancellationToken = default)
    {
        var ingredients = await _recipeIngredientRepository.GetAllByFamilyIdAsync(recipe.Id, cancellationToken);
        var steps = await _recipeStepRepository.GetAllByFamilyIdAsync(recipe.Id, cancellationToken);

        return new RecipeResponse
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Backstory = recipe.Backstory,
            FinalTime = recipe.FinalTime,
            Portions = recipe.Portions,
            RecipeBookId = recipe.RecipeBookId,
            Ingredients = ingredients.Select(ingredient => new RecipeIngredientResponse
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                MeasureUnit = ingredient.MeasureUnit,
                Quantity = ingredient.Quantity,
                RecipeId = ingredient.RecipeId,
            }).ToList(),
            Steps = steps.Select(step => new RecipeStepResponse
            {
                Id = step.Id,
                Order = step.Order,
                Description = step.Description,
                RecipeId = step.RecipeId,
            }).ToList(),
        };
    }
}
