namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IRecipeResponseMapper
{
    Task<RecipeResponse> MapAsync(
        Recipe recipe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps a set of recipes, loading their ingredients and steps with a single batch query each
    /// (avoids the N+1 reads produced by mapping recipes one by one).
    /// </summary>
    Task<IReadOnlyList<RecipeResponse>> MapAsync(
        IReadOnlyList<Recipe> recipes,
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

    /// <inheritdoc/>
    public async Task<RecipeResponse> MapAsync(
        Recipe recipe,
        CancellationToken cancellationToken = default)
    {
        var ingredients = await _recipeIngredientRepository.GetAllByRecipeIdAsync(recipe.Id, cancellationToken);
        var steps = await _recipeStepRepository.GetAllByRecipeIdAsync(recipe.Id, cancellationToken);
        return Map(recipe, ingredients, steps);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecipeResponse>> MapAsync(
        IReadOnlyList<Recipe> recipes,
        CancellationToken cancellationToken = default)
    {
        if (recipes.Count == 0)
        {
            return [];
        }

        var recipeIds = recipes.Select(recipe => recipe.Id).ToArray();

        var ingredientsByRecipeId = (await _recipeIngredientRepository.GetAllByRecipeIdsAsync(recipeIds, cancellationToken))
            .GroupBy(ingredient => ingredient.RecipeId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<RecipeIngredient>)group.ToList());

        var stepsByRecipeId = (await _recipeStepRepository.GetAllByRecipeIdsAsync(recipeIds, cancellationToken))
            .GroupBy(step => step.RecipeId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<RecipeStep>)group.ToList());

        return recipes
            .Select(recipe => Map(
                recipe,
                ingredientsByRecipeId.TryGetValue(recipe.Id, out var ingredients) ? ingredients : [],
                stepsByRecipeId.TryGetValue(recipe.Id, out var steps) ? steps : []))
            .ToList();
    }

    private static RecipeResponse Map(
        Recipe recipe,
        IReadOnlyList<RecipeIngredient> ingredients,
        IReadOnlyList<RecipeStep> steps) =>
        new()
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
