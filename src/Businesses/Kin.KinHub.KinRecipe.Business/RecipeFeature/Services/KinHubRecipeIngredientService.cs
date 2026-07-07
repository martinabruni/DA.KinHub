using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class KinHubRecipeIngredientService : IRecipeIngredientService
{
    private readonly ICreateRecipeIngredientHandler _createRecipeIngredientHandler;
    private readonly IGetRecipeIngredientsHandler _getRecipeIngredientsHandler;
    private readonly IGetRecipeIngredientByIdHandler _getRecipeIngredientByIdHandler;
    private readonly IUpdateRecipeIngredientHandler _updateRecipeIngredientHandler;
    private readonly IDeleteRecipeIngredientHandler _deleteRecipeIngredientHandler;

    public KinHubRecipeIngredientService(
        ICreateRecipeIngredientHandler createRecipeIngredientHandler,
        IGetRecipeIngredientsHandler getRecipeIngredientsHandler,
        IGetRecipeIngredientByIdHandler getRecipeIngredientByIdHandler,
        IUpdateRecipeIngredientHandler updateRecipeIngredientHandler,
        IDeleteRecipeIngredientHandler deleteRecipeIngredientHandler)
    {
        _createRecipeIngredientHandler = createRecipeIngredientHandler;
        _getRecipeIngredientsHandler = getRecipeIngredientsHandler;
        _getRecipeIngredientByIdHandler = getRecipeIngredientByIdHandler;
        _updateRecipeIngredientHandler = updateRecipeIngredientHandler;
        _deleteRecipeIngredientHandler = deleteRecipeIngredientHandler;
    }

    public Task<Result<RecipeIngredientResponse>> CreateAsync(
        CreateRecipeIngredientRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _createRecipeIngredientHandler.HandleAsync(request, userId, cancellationToken);

    public Task<Result<IReadOnlyList<RecipeIngredientResponse>>> GetAllAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getRecipeIngredientsHandler.HandleAsync(recipeId, userId, cancellationToken);

    public Task<Result<RecipeIngredientResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getRecipeIngredientByIdHandler.HandleAsync(id, userId, cancellationToken);

    public Task<Result<RecipeIngredientResponse>> UpdateAsync(
        Guid id,
        UpdateRecipeIngredientRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _updateRecipeIngredientHandler.HandleAsync(id, request, userId, cancellationToken);

    public Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _deleteRecipeIngredientHandler.HandleAsync(id, userId, cancellationToken);
}
