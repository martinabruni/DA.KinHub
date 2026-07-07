using Microsoft.Extensions.Logging;

namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class RecipeStepAccessService : IRecipeStepAccessService
{
    private readonly IFamilyRepository _familyRepository;
    private readonly IRecipeStepRepository _recipeStepRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeBookRepository _recipeBookRepository;
    private readonly ILogger<RecipeStepAccessService> _logger;

    public RecipeStepAccessService(
        IFamilyRepository familyRepository,
        IRecipeStepRepository recipeStepRepository,
        IRecipeRepository recipeRepository,
        IRecipeBookRepository recipeBookRepository,
        ILogger<RecipeStepAccessService> logger)
    {
        _familyRepository = familyRepository;
        _recipeStepRepository = recipeStepRepository;
        _recipeRepository = recipeRepository;
        _recipeBookRepository = recipeBookRepository;
        _logger = logger;
    }

    public async Task<RecipeStepAccessResult> GetAccessibleRecipeStepAsync(
        Guid recipeStepId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
        {
            _logger.LogWarning("Recipe step access failed because no family was found for user {UserId}.", userId);
            return RecipeStepAccessResult.NotFound("Family not found for the current user.");
        }

        var recipeStep = await _recipeStepRepository.GetByIdAsync(recipeStepId, cancellationToken);
        if (recipeStep is null)
        {
            _logger.LogWarning("Recipe step {RecipeStepId} was not found for user {UserId}.", recipeStepId, userId);
            return RecipeStepAccessResult.NotFound("Recipe step not found.");
        }

        var recipe = await _recipeRepository.GetByIdAsync(recipeStep.RecipeId, cancellationToken);
        var recipeBook = recipe is null
            ? null
            : await _recipeBookRepository.GetByIdAsync(recipe.RecipeBookId, cancellationToken);

        if (recipeBook is null || recipeBook.FamilyId != family.Id)
        {
            _logger.LogWarning(
                "Recipe step access denied for user {UserId}. Recipe step {RecipeStepId} resolves outside family {FamilyId}.",
                userId,
                recipeStepId,
                family.Id);
            return RecipeStepAccessResult.Unauthorized("Access denied.");
        }

        return RecipeStepAccessResult.Success(recipeStep);
    }
}
