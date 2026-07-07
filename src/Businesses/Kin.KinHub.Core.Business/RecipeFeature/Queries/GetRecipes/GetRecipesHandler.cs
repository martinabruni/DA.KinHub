namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class GetRecipesHandler : IGetRecipesHandler
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeBookAccessService _recipeBookAccessService;
    private readonly IRecipeResponseMapper _recipeResponseMapper;

    public GetRecipesHandler(
        IRecipeRepository recipeRepository,
        IRecipeBookAccessService recipeBookAccessService,
        IRecipeResponseMapper recipeResponseMapper)
    {
        _recipeRepository = recipeRepository;
        _recipeBookAccessService = recipeBookAccessService;
        _recipeResponseMapper = recipeResponseMapper;
    }

    public async Task<Result<IReadOnlyList<RecipeResponse>>> HandleAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeBookAccessService.GetAccessibleRecipeBookAsync(recipeBookId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<IReadOnlyList<RecipeResponse>>();
        }

        var recipes = await _recipeRepository.GetAllByRecipeBookIdAsync(recipeBookId, cancellationToken);
        var responses = await _recipeResponseMapper.MapAsync(recipes, cancellationToken);

        return Result<IReadOnlyList<RecipeResponse>>.Success(responses);
    }
}
