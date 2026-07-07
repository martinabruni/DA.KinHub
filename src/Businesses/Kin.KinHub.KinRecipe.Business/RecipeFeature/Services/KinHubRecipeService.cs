using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class KinHubRecipeService : IRecipeService
{
    private readonly ICreateRecipeHandler _createRecipeHandler;
    private readonly IGetRecipesHandler _getRecipesHandler;
    private readonly IGetRecipeByIdHandler _getRecipeByIdHandler;
    private readonly IUpdateRecipeHandler _updateRecipeHandler;
    private readonly IDeleteRecipeHandler _deleteRecipeHandler;

    public KinHubRecipeService(
        ICreateRecipeHandler createRecipeHandler,
        IGetRecipesHandler getRecipesHandler,
        IGetRecipeByIdHandler getRecipeByIdHandler,
        IUpdateRecipeHandler updateRecipeHandler,
        IDeleteRecipeHandler deleteRecipeHandler)
    {
        _createRecipeHandler = createRecipeHandler;
        _getRecipesHandler = getRecipesHandler;
        _getRecipeByIdHandler = getRecipeByIdHandler;
        _updateRecipeHandler = updateRecipeHandler;
        _deleteRecipeHandler = deleteRecipeHandler;
    }

    public Task<Result<RecipeResponse>> CreateAsync(
        CreateRecipeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _createRecipeHandler.HandleAsync(request, userId, cancellationToken);

    public Task<Result<IReadOnlyList<RecipeResponse>>> GetAllAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getRecipesHandler.HandleAsync(recipeBookId, userId, cancellationToken);

    public Task<Result<RecipeResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getRecipeByIdHandler.HandleAsync(id, userId, cancellationToken);

    public Task<Result<RecipeResponse>> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _updateRecipeHandler.HandleAsync(id, request, userId, cancellationToken);

    public Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _deleteRecipeHandler.HandleAsync(id, userId, cancellationToken);
}
