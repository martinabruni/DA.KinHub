using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class KinHubRecipeStepService : IRecipeStepService
{
    private readonly ICreateRecipeStepHandler _createRecipeStepHandler;
    private readonly IGetRecipeStepsHandler _getRecipeStepsHandler;
    private readonly IGetRecipeStepByIdHandler _getRecipeStepByIdHandler;
    private readonly IUpdateRecipeStepHandler _updateRecipeStepHandler;
    private readonly IDeleteRecipeStepHandler _deleteRecipeStepHandler;

    public KinHubRecipeStepService(
        ICreateRecipeStepHandler createRecipeStepHandler,
        IGetRecipeStepsHandler getRecipeStepsHandler,
        IGetRecipeStepByIdHandler getRecipeStepByIdHandler,
        IUpdateRecipeStepHandler updateRecipeStepHandler,
        IDeleteRecipeStepHandler deleteRecipeStepHandler)
    {
        _createRecipeStepHandler = createRecipeStepHandler;
        _getRecipeStepsHandler = getRecipeStepsHandler;
        _getRecipeStepByIdHandler = getRecipeStepByIdHandler;
        _updateRecipeStepHandler = updateRecipeStepHandler;
        _deleteRecipeStepHandler = deleteRecipeStepHandler;
    }

    public Task<Result<RecipeStepResponse>> CreateAsync(
        CreateRecipeStepRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _createRecipeStepHandler.HandleAsync(request, userId, cancellationToken);

    public Task<Result<IReadOnlyList<RecipeStepResponse>>> GetAllAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getRecipeStepsHandler.HandleAsync(recipeId, userId, cancellationToken);

    public Task<Result<RecipeStepResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getRecipeStepByIdHandler.HandleAsync(id, userId, cancellationToken);

    public Task<Result<RecipeStepResponse>> UpdateAsync(
        Guid id,
        UpdateRecipeStepRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _updateRecipeStepHandler.HandleAsync(id, request, userId, cancellationToken);

    public Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _deleteRecipeStepHandler.HandleAsync(id, userId, cancellationToken);
}
