namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipeByIdHandler
{
    Task<Result<RecipeResponse>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class GetRecipeByIdHandler : IGetRecipeByIdHandler
{
    private readonly IRecipeAccessService _recipeAccessService;
    private readonly IRecipeResponseMapper _recipeResponseMapper;

    public GetRecipeByIdHandler(
        IRecipeAccessService recipeAccessService,
        IRecipeResponseMapper recipeResponseMapper)
    {
        _recipeAccessService = recipeAccessService;
        _recipeResponseMapper = recipeResponseMapper;
    }

    public async Task<Result<RecipeResponse>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeAccessService.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);
        if (!access.IsSuccess)
            return access.ToResult<RecipeResponse>();

        return Result<RecipeResponse>.Success(
            await _recipeResponseMapper.MapAsync(access.Recipe!, cancellationToken));
    }
}
