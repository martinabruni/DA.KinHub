namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class CreateRecipeBookHandler : ICreateRecipeBookHandler
{
    private readonly IRecipeBookRepository _recipeBookRepository;
    private readonly IFamilyOwnershipService _familyOwnershipService;
    private readonly IRecipeBookResponseMapper _recipeBookResponseMapper;

    public CreateRecipeBookHandler(
        IRecipeBookRepository recipeBookRepository,
        IFamilyOwnershipService familyOwnershipService,
        IRecipeBookResponseMapper recipeBookResponseMapper)
    {
        _recipeBookRepository = recipeBookRepository;
        _familyOwnershipService = familyOwnershipService;
        _recipeBookResponseMapper = recipeBookResponseMapper;
    }

    public async Task<Result<RecipeBookResponse>> HandleAsync(
        CreateRecipeBookRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var familyAccess = await _familyOwnershipService.GetCurrentFamilyAsync(userId, cancellationToken);
        if (!familyAccess.IsSuccess)
        {
            return familyAccess.ToResult<RecipeBookResponse>();
        }

        var now = DateTime.UtcNow;
        var recipeBook = new RecipeBook
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            FamilyId = familyAccess.Family!.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var createdRecipeBook = await _recipeBookRepository.AddAsync(recipeBook, cancellationToken);
        return Result<RecipeBookResponse>.Success(_recipeBookResponseMapper.Map(createdRecipeBook));
    }
}
