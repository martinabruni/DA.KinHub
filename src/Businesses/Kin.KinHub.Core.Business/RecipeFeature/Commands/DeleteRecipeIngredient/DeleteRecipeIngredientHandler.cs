namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class DeleteRecipeIngredientHandler : IDeleteRecipeIngredientHandler
{
    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IRecipeIngredientAccessService _recipeIngredientAccessService;

    public DeleteRecipeIngredientHandler(
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeIngredientAccessService recipeIngredientAccessService)
    {
        _recipeIngredientRepository = recipeIngredientRepository;
        _recipeIngredientAccessService = recipeIngredientAccessService;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid recipeIngredientId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeIngredientAccessService.GetAccessibleRecipeIngredientAsync(recipeIngredientId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<bool>();
        }

        await _recipeIngredientRepository.SoftDeleteAsync(recipeIngredientId, cancellationToken);
        return Result<bool>.Success(true);
    }
}
