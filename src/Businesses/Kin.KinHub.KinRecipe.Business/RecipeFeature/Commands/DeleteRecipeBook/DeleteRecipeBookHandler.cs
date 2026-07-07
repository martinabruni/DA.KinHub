namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class DeleteRecipeBookHandler : IDeleteRecipeBookHandler
{
    private readonly IRecipeBookRepository _recipeBookRepository;
    private readonly IRecipeBookAccessService _recipeBookAccessService;

    public DeleteRecipeBookHandler(
        IRecipeBookRepository recipeBookRepository,
        IRecipeBookAccessService recipeBookAccessService)
    {
        _recipeBookRepository = recipeBookRepository;
        _recipeBookAccessService = recipeBookAccessService;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeBookAccessService.GetAccessibleRecipeBookAsync(recipeBookId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<bool>();
        }

        await _recipeBookRepository.SoftDeleteAsync(recipeBookId, cancellationToken);
        return Result<bool>.Success(true);
    }
}
