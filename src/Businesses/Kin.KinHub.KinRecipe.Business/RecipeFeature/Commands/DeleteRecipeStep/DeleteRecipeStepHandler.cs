namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class DeleteRecipeStepHandler : IDeleteRecipeStepHandler
{
    private readonly IRecipeStepRepository _recipeStepRepository;
    private readonly IRecipeStepAccessService _recipeStepAccessService;

    public DeleteRecipeStepHandler(
        IRecipeStepRepository recipeStepRepository,
        IRecipeStepAccessService recipeStepAccessService)
    {
        _recipeStepRepository = recipeStepRepository;
        _recipeStepAccessService = recipeStepAccessService;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid recipeStepId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeStepAccessService.GetAccessibleRecipeStepAsync(recipeStepId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<bool>();
        }

        await _recipeStepRepository.SoftDeleteAsync(recipeStepId, cancellationToken);
        return Result<bool>.Success(true);
    }
}
