namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class GetRecipeBooksHandler : IGetRecipeBooksHandler
{
    private readonly IRecipeBookRepository _recipeBookRepository;
    private readonly IFamilyOwnershipService _familyOwnershipService;
    private readonly IRecipeBookResponseMapper _recipeBookResponseMapper;

    public GetRecipeBooksHandler(
        IRecipeBookRepository recipeBookRepository,
        IFamilyOwnershipService familyOwnershipService,
        IRecipeBookResponseMapper recipeBookResponseMapper)
    {
        _recipeBookRepository = recipeBookRepository;
        _familyOwnershipService = familyOwnershipService;
        _recipeBookResponseMapper = recipeBookResponseMapper;
    }

    public async Task<Result<IReadOnlyList<RecipeBookResponse>>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var familyAccess = await _familyOwnershipService.GetCurrentFamilyAsync(userId, cancellationToken);
        if (!familyAccess.IsSuccess)
        {
            return familyAccess.ToResult<IReadOnlyList<RecipeBookResponse>>();
        }

        var recipeBooks = await _recipeBookRepository.GetAllByFamilyIdAsync(familyAccess.Family!.Id, cancellationToken);
        return Result<IReadOnlyList<RecipeBookResponse>>.Success(recipeBooks.Select(_recipeBookResponseMapper.Map).ToList());
    }
}
