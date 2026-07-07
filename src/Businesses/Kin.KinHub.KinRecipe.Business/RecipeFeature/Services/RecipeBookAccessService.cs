using Microsoft.Extensions.Logging;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class RecipeBookAccessService : IRecipeBookAccessService
{
    private readonly IFamilyRepository _familyRepository;
    private readonly IRecipeBookRepository _recipeBookRepository;
    private readonly ILogger<RecipeBookAccessService> _logger;

    public RecipeBookAccessService(
        IFamilyRepository familyRepository,
        IRecipeBookRepository recipeBookRepository,
        ILogger<RecipeBookAccessService> logger)
    {
        _familyRepository = familyRepository;
        _recipeBookRepository = recipeBookRepository;
        _logger = logger;
    }

    public async Task<RecipeBookAccessResult> GetAccessibleRecipeBookAsync(
        Guid recipeBookId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
        {
            _logger.LogWarning("Recipe book access failed because no family was found for user {UserId}.", userId);
            return RecipeBookAccessResult.NotFound("Family not found for the current user.");
        }

        var recipeBook = await _recipeBookRepository.GetByIdAsync(recipeBookId, cancellationToken);
        if (recipeBook is null)
        {
            _logger.LogWarning("Recipe book {RecipeBookId} was not found for user {UserId}.", recipeBookId, userId);
            return RecipeBookAccessResult.NotFound("Recipe book not found.");
        }

        if (recipeBook.FamilyId != family.Id)
        {
            _logger.LogWarning(
                "Recipe book access denied for user {UserId}. Recipe book {RecipeBookId} belongs to family {RecipeBookFamilyId}, user family {UserFamilyId}.",
                userId,
                recipeBookId,
                recipeBook.FamilyId,
                family.Id);
            return RecipeBookAccessResult.Unauthorized("Access denied.");
        }

        return RecipeBookAccessResult.Success(family, recipeBook);
    }
}
