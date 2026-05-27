namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipesHandler
{
    Task<Result<IReadOnlyList<RecipeResponse>>> HandleAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

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
            return access.ToResult<IReadOnlyList<RecipeResponse>>();

        var recipes = await _recipeRepository.GetAllByFamilyIdAsync(recipeBookId, cancellationToken);
        var responses = new List<RecipeResponse>(recipes.Count);
        foreach (var recipe in recipes)
            responses.Add(await _recipeResponseMapper.MapAsync(recipe, cancellationToken));

        return Result<IReadOnlyList<RecipeResponse>>.Success(responses);
    }
}
