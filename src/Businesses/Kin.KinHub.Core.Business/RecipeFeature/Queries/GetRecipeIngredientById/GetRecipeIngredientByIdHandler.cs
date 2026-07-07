namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class GetRecipeIngredientByIdHandler : IGetRecipeIngredientByIdHandler
{
    private readonly IRecipeIngredientAccessService _recipeIngredientAccessService;
    private readonly IRecipeIngredientResponseMapper _recipeIngredientResponseMapper;

    public GetRecipeIngredientByIdHandler(
        IRecipeIngredientAccessService recipeIngredientAccessService,
        IRecipeIngredientResponseMapper recipeIngredientResponseMapper)
    {
        _recipeIngredientAccessService = recipeIngredientAccessService;
        _recipeIngredientResponseMapper = recipeIngredientResponseMapper;
    }

    public async Task<Result<RecipeIngredientResponse>> HandleAsync(
        Guid recipeIngredientId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeIngredientAccessService.GetAccessibleRecipeIngredientAsync(recipeIngredientId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<RecipeIngredientResponse>();
        }

        return Result<RecipeIngredientResponse>.Success(_recipeIngredientResponseMapper.Map(access.RecipeIngredient!));
    }
}
