namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class GetRecipeIngredientsHandler : IGetRecipeIngredientsHandler
{
    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IRecipeAccessService _recipeAccessService;
    private readonly IRecipeIngredientResponseMapper _recipeIngredientResponseMapper;

    public GetRecipeIngredientsHandler(
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeAccessService recipeAccessService,
        IRecipeIngredientResponseMapper recipeIngredientResponseMapper)
    {
        _recipeIngredientRepository = recipeIngredientRepository;
        _recipeAccessService = recipeAccessService;
        _recipeIngredientResponseMapper = recipeIngredientResponseMapper;
    }

    public async Task<Result<IReadOnlyList<RecipeIngredientResponse>>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeAccessService.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<IReadOnlyList<RecipeIngredientResponse>>();
        }

        var recipeIngredients = await _recipeIngredientRepository.GetAllByRecipeIdAsync(recipeId, cancellationToken);
        return Result<IReadOnlyList<RecipeIngredientResponse>>.Success(recipeIngredients.Select(_recipeIngredientResponseMapper.Map).ToList());
    }
}
