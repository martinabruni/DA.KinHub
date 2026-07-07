namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class CreateRecipeHandler : ICreateRecipeHandler
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IRecipeStepRepository _recipeStepRepository;
    private readonly IRecipeBookAccessService _recipeBookAccessService;
    private readonly IRecipeResponseMapper _recipeResponseMapper;
    private readonly ICoreTransactionExecutor _transactionExecutor;

    public CreateRecipeHandler(
        IRecipeRepository recipeRepository,
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeStepRepository recipeStepRepository,
        IRecipeBookAccessService recipeBookAccessService,
        IRecipeResponseMapper recipeResponseMapper,
        ICoreTransactionExecutor transactionExecutor)
    {
        _recipeRepository = recipeRepository;
        _recipeIngredientRepository = recipeIngredientRepository;
        _recipeStepRepository = recipeStepRepository;
        _recipeBookAccessService = recipeBookAccessService;
        _recipeResponseMapper = recipeResponseMapper;
        _transactionExecutor = transactionExecutor;
    }

    public async Task<Result<RecipeResponse>> HandleAsync(
        CreateRecipeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeBookAccessService.GetAccessibleRecipeBookAsync(request.RecipeBookId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<RecipeResponse>();
        }

        return await _transactionExecutor.ExecuteAsync(async ct =>
        {
            var now = DateTime.UtcNow;
            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Backstory = request.Backstory,
                FinalTime = request.FinalTime,
                Portions = request.Portions,
                RecipeBookId = request.RecipeBookId,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var createdRecipe = await _recipeRepository.AddAsync(recipe, ct);

            if (request.Ingredients is { Count: > 0 })
            {
                var ingredients = request.Ingredients.Select(ingredient => new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    Name = ingredient.Name,
                    MeasureUnit = ingredient.MeasureUnit,
                    Quantity = ingredient.Quantity,
                    RecipeId = createdRecipe.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToArray();

                await _recipeIngredientRepository.AddRangeAsync(ingredients, ct);
            }

            if (request.Steps is { Count: > 0 })
            {
                var steps = request.Steps.Select(step => new RecipeStep
                {
                    Id = Guid.NewGuid(),
                    Order = step.Order,
                    Description = step.Description,
                    RecipeId = createdRecipe.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToArray();

                await _recipeStepRepository.AddRangeAsync(steps, ct);
            }

            return Result<RecipeResponse>.Success(
                await _recipeResponseMapper.MapAsync(createdRecipe, ct));
        }, cancellationToken);
    }
}
