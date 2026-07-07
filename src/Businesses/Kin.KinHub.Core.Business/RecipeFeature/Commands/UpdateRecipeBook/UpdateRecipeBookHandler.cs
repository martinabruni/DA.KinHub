namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class UpdateRecipeBookHandler : IUpdateRecipeBookHandler
{
    private readonly IRecipeBookRepository _recipeBookRepository;
    private readonly IRecipeBookAccessService _recipeBookAccessService;
    private readonly IRecipeBookResponseMapper _recipeBookResponseMapper;

    public UpdateRecipeBookHandler(
        IRecipeBookRepository recipeBookRepository,
        IRecipeBookAccessService recipeBookAccessService,
        IRecipeBookResponseMapper recipeBookResponseMapper)
    {
        _recipeBookRepository = recipeBookRepository;
        _recipeBookAccessService = recipeBookAccessService;
        _recipeBookResponseMapper = recipeBookResponseMapper;
    }

    public async Task<Result<RecipeBookResponse>> HandleAsync(
        Guid recipeBookId,
        UpdateRecipeBookRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeBookAccessService.GetAccessibleRecipeBookAsync(recipeBookId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<RecipeBookResponse>();
        }

        var recipeBook = access.RecipeBook!;
        recipeBook.Name = request.Name;
        recipeBook.Description = request.Description;
        recipeBook.UpdatedAt = DateTime.UtcNow;

        var updatedRecipeBook = await _recipeBookRepository.UpdateAsync(recipeBook, cancellationToken);
        return Result<RecipeBookResponse>.Success(_recipeBookResponseMapper.Map(updatedRecipeBook));
    }
}
