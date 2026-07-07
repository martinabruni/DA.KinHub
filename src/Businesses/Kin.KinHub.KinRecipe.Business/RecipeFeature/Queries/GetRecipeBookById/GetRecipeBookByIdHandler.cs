namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class GetRecipeBookByIdHandler : IGetRecipeBookByIdHandler
{
    private readonly IRecipeBookAccessService _recipeBookAccessService;
    private readonly IRecipeBookResponseMapper _recipeBookResponseMapper;

    public GetRecipeBookByIdHandler(
        IRecipeBookAccessService recipeBookAccessService,
        IRecipeBookResponseMapper recipeBookResponseMapper)
    {
        _recipeBookAccessService = recipeBookAccessService;
        _recipeBookResponseMapper = recipeBookResponseMapper;
    }

    public async Task<Result<RecipeBookResponse>> HandleAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeBookAccessService.GetAccessibleRecipeBookAsync(recipeBookId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<RecipeBookResponse>();
        }

        return Result<RecipeBookResponse>.Success(_recipeBookResponseMapper.Map(access.RecipeBook!));
    }
}
