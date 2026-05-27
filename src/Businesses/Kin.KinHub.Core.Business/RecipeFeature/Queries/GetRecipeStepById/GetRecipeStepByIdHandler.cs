namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IGetRecipeStepByIdHandler
{
    Task<Result<RecipeStepResponse>> HandleAsync(
        Guid recipeStepId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class GetRecipeStepByIdHandler : IGetRecipeStepByIdHandler
{
    private readonly IRecipeStepAccessService _recipeStepAccessService;
    private readonly IRecipeStepResponseMapper _recipeStepResponseMapper;

    public GetRecipeStepByIdHandler(
        IRecipeStepAccessService recipeStepAccessService,
        IRecipeStepResponseMapper recipeStepResponseMapper)
    {
        _recipeStepAccessService = recipeStepAccessService;
        _recipeStepResponseMapper = recipeStepResponseMapper;
    }

    public async Task<Result<RecipeStepResponse>> HandleAsync(
        Guid recipeStepId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeStepAccessService.GetAccessibleRecipeStepAsync(recipeStepId, userId, cancellationToken);
        if (!access.IsSuccess)
            return access.ToResult<RecipeStepResponse>();

        return Result<RecipeStepResponse>.Success(_recipeStepResponseMapper.Map(access.RecipeStep!));
    }
}
