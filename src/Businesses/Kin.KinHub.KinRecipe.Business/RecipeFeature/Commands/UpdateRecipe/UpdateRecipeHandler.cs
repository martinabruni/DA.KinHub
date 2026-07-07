namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class UpdateRecipeHandler : IUpdateRecipeHandler
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeAccessService _recipeAccessService;
    private readonly IRecipeResponseMapper _recipeResponseMapper;

    public UpdateRecipeHandler(
        IRecipeRepository recipeRepository,
        IRecipeAccessService recipeAccessService,
        IRecipeResponseMapper recipeResponseMapper)
    {
        _recipeRepository = recipeRepository;
        _recipeAccessService = recipeAccessService;
        _recipeResponseMapper = recipeResponseMapper;
    }

    public async Task<Result<RecipeResponse>> HandleAsync(
        Guid recipeId,
        UpdateRecipeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeAccessService.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<RecipeResponse>();
        }

        var recipe = access.Recipe!;
        recipe.Name = request.Name;
        recipe.Backstory = request.Backstory;
        recipe.FinalTime = request.FinalTime;
        recipe.Portions = request.Portions;
        recipe.UpdatedAt = DateTime.UtcNow;

        var updatedRecipe = await _recipeRepository.UpdateAsync(recipe, cancellationToken);
        return Result<RecipeResponse>.Success(
            await _recipeResponseMapper.MapAsync(updatedRecipe, cancellationToken));
    }
}
