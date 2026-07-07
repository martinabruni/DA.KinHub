using Microsoft.Extensions.Logging;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class RecipeAccessService : IRecipeAccessService
{
    private readonly IFamilyRepository _familyRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeBookRepository _recipeBookRepository;
    private readonly ILogger<RecipeAccessService> _logger;

    public RecipeAccessService(
        IFamilyRepository familyRepository,
        IRecipeRepository recipeRepository,
        IRecipeBookRepository recipeBookRepository,
        ILogger<RecipeAccessService> logger)
    {
        _familyRepository = familyRepository;
        _recipeRepository = recipeRepository;
        _recipeBookRepository = recipeBookRepository;
        _logger = logger;
    }

    public async Task<RecipeAccessResult> GetAccessibleRecipeAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
        {
            _logger.LogWarning("Recipe access failed because no family was found for user {UserId}.", userId);
            return RecipeAccessResult.NotFound("Family not found for the current user.");
        }

        var recipe = await _recipeRepository.GetByIdAsync(recipeId, cancellationToken);
        if (recipe is null)
        {
            _logger.LogWarning("Recipe {RecipeId} was not found for user {UserId}.", recipeId, userId);
            return RecipeAccessResult.NotFound("Recipe not found.");
        }

        var recipeBook = await _recipeBookRepository.GetByIdAsync(recipe.RecipeBookId, cancellationToken);
        if (recipeBook is null || recipeBook.FamilyId != family.Id)
        {
            _logger.LogWarning(
                "Recipe access denied for user {UserId}. Recipe {RecipeId} is linked to recipe book {RecipeBookId} outside family {FamilyId}.",
                userId,
                recipeId,
                recipe.RecipeBookId,
                family.Id);
            return RecipeAccessResult.Unauthorized("Access denied.");
        }

        return RecipeAccessResult.Success(family, recipe, recipeBook);
    }
}
