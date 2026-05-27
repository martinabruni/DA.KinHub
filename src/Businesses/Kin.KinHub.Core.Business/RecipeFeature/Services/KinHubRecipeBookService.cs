using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class KinHubRecipeBookService : IRecipeBookService
{
    private readonly ICreateRecipeBookHandler _createRecipeBookHandler;
    private readonly IGetRecipeBooksHandler _getRecipeBooksHandler;
    private readonly IGetRecipeBookByIdHandler _getRecipeBookByIdHandler;
    private readonly IUpdateRecipeBookHandler _updateRecipeBookHandler;
    private readonly IDeleteRecipeBookHandler _deleteRecipeBookHandler;

    public KinHubRecipeBookService(
        ICreateRecipeBookHandler createRecipeBookHandler,
        IGetRecipeBooksHandler getRecipeBooksHandler,
        IGetRecipeBookByIdHandler getRecipeBookByIdHandler,
        IUpdateRecipeBookHandler updateRecipeBookHandler,
        IDeleteRecipeBookHandler deleteRecipeBookHandler)
    {
        _createRecipeBookHandler = createRecipeBookHandler;
        _getRecipeBooksHandler = getRecipeBooksHandler;
        _getRecipeBookByIdHandler = getRecipeBookByIdHandler;
        _updateRecipeBookHandler = updateRecipeBookHandler;
        _deleteRecipeBookHandler = deleteRecipeBookHandler;
    }

    public Task<Result<RecipeBookResponse>> CreateAsync(
        CreateRecipeBookRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _createRecipeBookHandler.HandleAsync(request, userId, cancellationToken);

    public Task<Result<IReadOnlyList<RecipeBookResponse>>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getRecipeBooksHandler.HandleAsync(userId, cancellationToken);

    public Task<Result<RecipeBookResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getRecipeBookByIdHandler.HandleAsync(id, userId, cancellationToken);

    public Task<Result<RecipeBookResponse>> UpdateAsync(
        Guid id,
        UpdateRecipeBookRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _updateRecipeBookHandler.HandleAsync(id, request, userId, cancellationToken);

    public Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _deleteRecipeBookHandler.HandleAsync(id, userId, cancellationToken);
}
