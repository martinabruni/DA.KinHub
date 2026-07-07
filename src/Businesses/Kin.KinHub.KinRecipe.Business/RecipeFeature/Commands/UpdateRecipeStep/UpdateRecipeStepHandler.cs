namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class UpdateRecipeStepHandler : IUpdateRecipeStepHandler
{
    private readonly IRecipeStepRepository _recipeStepRepository;
    private readonly IRecipeStepAccessService _recipeStepAccessService;
    private readonly IRecipeStepResponseMapper _recipeStepResponseMapper;

    public UpdateRecipeStepHandler(
        IRecipeStepRepository recipeStepRepository,
        IRecipeStepAccessService recipeStepAccessService,
        IRecipeStepResponseMapper recipeStepResponseMapper)
    {
        _recipeStepRepository = recipeStepRepository;
        _recipeStepAccessService = recipeStepAccessService;
        _recipeStepResponseMapper = recipeStepResponseMapper;
    }

    public async Task<Result<RecipeStepResponse>> HandleAsync(
        Guid recipeStepId,
        UpdateRecipeStepRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeStepAccessService.GetAccessibleRecipeStepAsync(recipeStepId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<RecipeStepResponse>();
        }

        var recipeStep = access.RecipeStep!;
        recipeStep.Order = request.Order;
        recipeStep.Description = request.Description;
        recipeStep.UpdatedAt = DateTime.UtcNow;

        var updatedRecipeStep = await _recipeStepRepository.UpdateAsync(recipeStep, cancellationToken);
        return Result<RecipeStepResponse>.Success(_recipeStepResponseMapper.Map(updatedRecipeStep));
    }
}
