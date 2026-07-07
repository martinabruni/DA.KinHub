namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class DeleteRecipeHandler : IDeleteRecipeHandler
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeAccessService _recipeAccessService;

    public DeleteRecipeHandler(
        IRecipeRepository recipeRepository,
        IRecipeAccessService recipeAccessService)
    {
        _recipeRepository = recipeRepository;
        _recipeAccessService = recipeAccessService;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeAccessService.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<bool>();
        }

        await _recipeRepository.SoftDeleteAsync(recipeId, cancellationToken);
        return Result<bool>.Success(true);
    }
}
