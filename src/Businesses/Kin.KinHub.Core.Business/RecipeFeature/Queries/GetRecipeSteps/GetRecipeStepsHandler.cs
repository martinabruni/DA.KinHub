namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipeStepsHandler
{
    Task<Result<IReadOnlyList<RecipeStepResponse>>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class GetRecipeStepsHandler : IGetRecipeStepsHandler
{
    private readonly IRecipeStepRepository _recipeStepRepository;
    private readonly IRecipeAccessService _recipeAccessService;
    private readonly IRecipeStepResponseMapper _recipeStepResponseMapper;

    public GetRecipeStepsHandler(
        IRecipeStepRepository recipeStepRepository,
        IRecipeAccessService recipeAccessService,
        IRecipeStepResponseMapper recipeStepResponseMapper)
    {
        _recipeStepRepository = recipeStepRepository;
        _recipeAccessService = recipeAccessService;
        _recipeStepResponseMapper = recipeStepResponseMapper;
    }

    public async Task<Result<IReadOnlyList<RecipeStepResponse>>> HandleAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeAccessService.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);
        if (!access.IsSuccess)
            return access.ToResult<IReadOnlyList<RecipeStepResponse>>();

        var recipeSteps = await _recipeStepRepository.GetAllByRecipeIdAsync(recipeId, cancellationToken);
        return Result<IReadOnlyList<RecipeStepResponse>>.Success(recipeSteps.Select(_recipeStepResponseMapper.Map).ToList());
    }
}
