using Microsoft.Extensions.Logging;

namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class RecipeIngredientAccessService : IRecipeIngredientAccessService
{
    private readonly IFamilyRepository _familyRepository;
    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeBookRepository _recipeBookRepository;
    private readonly ILogger<RecipeIngredientAccessService> _logger;

    public RecipeIngredientAccessService(
        IFamilyRepository familyRepository,
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeRepository recipeRepository,
        IRecipeBookRepository recipeBookRepository,
        ILogger<RecipeIngredientAccessService> logger)
    {
        _familyRepository = familyRepository;
        _recipeIngredientRepository = recipeIngredientRepository;
        _recipeRepository = recipeRepository;
        _recipeBookRepository = recipeBookRepository;
        _logger = logger;
    }

    public async Task<RecipeIngredientAccessResult> GetAccessibleRecipeIngredientAsync(
        Guid recipeIngredientId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
        {
            _logger.LogWarning("Recipe ingredient access failed because no family was found for user {UserId}.", userId);
            return RecipeIngredientAccessResult.NotFound("Family not found for the current user.");
        }

        var recipeIngredient = await _recipeIngredientRepository.GetByIdAsync(recipeIngredientId, cancellationToken);
        if (recipeIngredient is null)
        {
            _logger.LogWarning("Recipe ingredient {RecipeIngredientId} was not found for user {UserId}.", recipeIngredientId, userId);
            return RecipeIngredientAccessResult.NotFound("Recipe ingredient not found.");
        }

        var recipe = await _recipeRepository.GetByIdAsync(recipeIngredient.RecipeId, cancellationToken);
        var recipeBook = recipe is null
            ? null
            : await _recipeBookRepository.GetByIdAsync(recipe.RecipeBookId, cancellationToken);

        if (recipeBook is null || recipeBook.FamilyId != family.Id)
        {
            _logger.LogWarning(
                "Recipe ingredient access denied for user {UserId}. Recipe ingredient {RecipeIngredientId} resolves outside family {FamilyId}.",
                userId,
                recipeIngredientId,
                family.Id);
            return RecipeIngredientAccessResult.Unauthorized("Access denied.");
        }

        return RecipeIngredientAccessResult.Success(recipeIngredient);
    }
}
