namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface ICreateRecipeHandler
{
    Task<Result<RecipeResponse>> HandleAsync(
        CreateRecipeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class CreateRecipeHandler : ICreateRecipeHandler
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IRecipeStepRepository _recipeStepRepository;
    private readonly IRecipeBookAccessService _recipeBookAccessService;
    private readonly IRecipeResponseMapper _recipeResponseMapper;

    public CreateRecipeHandler(
        IRecipeRepository recipeRepository,
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeStepRepository recipeStepRepository,
        IRecipeBookAccessService recipeBookAccessService,
        IRecipeResponseMapper recipeResponseMapper)
    {
        _recipeRepository = recipeRepository;
        _recipeIngredientRepository = recipeIngredientRepository;
        _recipeStepRepository = recipeStepRepository;
        _recipeBookAccessService = recipeBookAccessService;
        _recipeResponseMapper = recipeResponseMapper;
    }

    public async Task<Result<RecipeResponse>> HandleAsync(
        CreateRecipeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeBookAccessService.GetAccessibleRecipeBookAsync(request.RecipeBookId, userId, cancellationToken);
        if (!access.IsSuccess)
            return access.ToResult<RecipeResponse>();

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

        var createdRecipe = await _recipeRepository.AddAsync(recipe, cancellationToken);

        if (request.Ingredients is { Count: > 0 })
        {
            foreach (var ingredient in request.Ingredients)
            {
                await _recipeIngredientRepository.AddAsync(new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    Name = ingredient.Name,
                    MeasureUnit = ingredient.MeasureUnit,
                    Quantity = ingredient.Quantity,
                    RecipeId = createdRecipe.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                }, cancellationToken);
            }
        }

        if (request.Steps is { Count: > 0 })
        {
            foreach (var step in request.Steps)
            {
                await _recipeStepRepository.AddAsync(new RecipeStep
                {
                    Id = Guid.NewGuid(),
                    Order = step.Order,
                    Description = step.Description,
                    RecipeId = createdRecipe.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                }, cancellationToken);
            }
        }

        return Result<RecipeResponse>.Success(
            await _recipeResponseMapper.MapAsync(createdRecipe, cancellationToken));
    }
}
