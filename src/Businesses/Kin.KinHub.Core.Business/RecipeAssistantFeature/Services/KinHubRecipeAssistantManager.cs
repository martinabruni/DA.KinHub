using Kin.KinHub.Core.Business.Common;
using Mapster;

namespace Kin.KinHub.Core.Business.RecipeAssistantFeature;

public sealed class KinHubRecipeAssistantManager : IRecipeAssistantManager
{
    private readonly IFamilyRepository _familyRepository;
    private readonly IFridgeRepository _fridgeRepository;
    private readonly IFridgeIngredientRepository _fridgeIngredientRepository;
    private readonly IRecipeBookRepository _recipeBookRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IRecipeStepRepository _recipeStepRepository;
    private readonly IRecipeAssistantService _recipeAssistantService;

    public KinHubRecipeAssistantManager(
        IFamilyRepository familyRepository,
        IFridgeRepository fridgeRepository,
        IFridgeIngredientRepository fridgeIngredientRepository,
        IRecipeBookRepository recipeBookRepository,
        IRecipeRepository recipeRepository,
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeStepRepository recipeStepRepository,
        IRecipeAssistantService recipeAssistantService)
    {
        _familyRepository = familyRepository;
        _fridgeRepository = fridgeRepository;
        _fridgeIngredientRepository = fridgeIngredientRepository;
        _recipeBookRepository = recipeBookRepository;
        _recipeRepository = recipeRepository;
        _recipeIngredientRepository = recipeIngredientRepository;
        _recipeStepRepository = recipeStepRepository;
        _recipeAssistantService = recipeAssistantService;
    }

    public async Task<Result<SuggestRecipesResult>> SuggestRecipesAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<SuggestRecipesResult>.NotFound("Family not found for the current user.");

        var fridge = await _fridgeRepository.GetByIdAsync(fridgeId, cancellationToken);
        if (fridge is null)
            return Result<SuggestRecipesResult>.NotFound("Fridge not found.");
        if (fridge.FamilyId != family.Id)
            return Result<SuggestRecipesResult>.Unauthorized("Access denied.");

        var fridgeIngredients = await _fridgeIngredientRepository.GetAllByFridgeIdAsync(fridgeId, cancellationToken);
        var fridgeLookup = fridgeIngredients
            .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity), StringComparer.OrdinalIgnoreCase);

        var books = await _recipeBookRepository.GetAllByFamilyIdAsync(family.Id, cancellationToken);
        var recipes = await _recipeRepository.GetAllByRecipeBookIdsAsync(books.Select(book => book.Id).ToArray(), cancellationToken);
        var ingredientsByRecipeId = (await _recipeIngredientRepository.GetAllByRecipeIdsAsync(recipes.Select(recipe => recipe.Id).ToArray(), cancellationToken))
            .GroupBy(ingredient => ingredient.RecipeId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<RecipeIngredient>)group.ToList());
        var existingRecipes = new List<ExistingRecipeSuggestionResponse>();

        foreach (var recipe in recipes)
        {
            recipe.Ingredients = ingredientsByRecipeId.TryGetValue(recipe.Id, out var recipeIngredients)
                ? recipeIngredients
                : [];
            if (recipe.Ingredients is null || recipe.Ingredients.Count == 0)
            {
                continue;
            }

            var missingIngredients = recipe.Ingredients
                .Where(ing =>
                    !fridgeLookup.TryGetValue(ing.Name, out var available) || available < ing.Quantity)
                .ToList();

            var matchPercentage = (int)Math.Round(
                (double)(recipe.Ingredients.Count - missingIngredients.Count) / recipe.Ingredients.Count * 100);

            existingRecipes.Add(new ExistingRecipeSuggestionResponse
            {
                RecipeId = recipe.Id,
                Name = recipe.Name,
                MatchPercentage = matchPercentage,
                MissingIngredients = missingIngredients
                    .Select(i => new AssistantIngredientResponse { Name = i.Name, Quantity = i.Quantity, MeasureUnit = i.MeasureUnit })
                    .ToList(),
            });
        }

        existingRecipes = existingRecipes.OrderByDescending(r => r.MatchPercentage).ToList();

        var fridgeAi = fridgeIngredients
            .Select(i => new RecipeIngredient { Id = Guid.Empty, Name = i.Name, Quantity = i.Quantity, MeasureUnit = i.MeasureUnit, RecipeId = Guid.Empty })
            .ToList();

        try
        {
            var newSuggestions = await _recipeAssistantService.SuggestNewRecipesAsync(fridgeAi, cancellationToken);
            return Result<SuggestRecipesResult>.Success(new SuggestRecipesResult
            {
                ExistingRecipes = existingRecipes,
                NewRecipes = newSuggestions.Adapt<IReadOnlyList<RecipeSuggestionResponse>>(),
            });
        }
        catch (RecipeAssistantInvalidResponseException ex)
        {
            return Result<SuggestRecipesResult>.UnprocessableEntity(ex.Message, "recipe_assistant_invalid_response");
        }
        catch (RecipeAssistantUnavailableException ex)
        {
            return Result<SuggestRecipesResult>.ServiceUnavailable(ex.Message, "recipe_assistant_unavailable");
        }
    }

    public async Task<Result<ParsedRecipeResponse?>> ParseRecipeAsync(
        string rawText,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recipe = await _recipeAssistantService.ParseRecipeAsync(rawText, cancellationToken);
            return Result<ParsedRecipeResponse?>.Success(recipe?.Adapt<ParsedRecipeResponse>());
        }
        catch (RecipeAssistantInvalidResponseException ex)
        {
            return Result<ParsedRecipeResponse?>.UnprocessableEntity(ex.Message, "recipe_assistant_invalid_response");
        }
        catch (RecipeAssistantUnavailableException ex)
        {
            return Result<ParsedRecipeResponse?>.ServiceUnavailable(ex.Message, "recipe_assistant_unavailable");
        }
    }

    public async Task<Result<RecipeAdaptationResponse>> AdaptRecipeAsync(
        Guid recipeId,
        IReadOnlyList<string> constraints,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<RecipeAdaptationResponse>.NotFound("Family not found for the current user.");

        var recipe = await _recipeRepository.GetByIdAsync(recipeId, cancellationToken);
        if (recipe is null)
            return Result<RecipeAdaptationResponse>.NotFound("Recipe not found.");

        var book = await _recipeBookRepository.GetByIdAsync(recipe.RecipeBookId, cancellationToken);
        if (book is null)
            return Result<RecipeAdaptationResponse>.NotFound("Recipe book not found.");
        if (book.FamilyId != family.Id)
            return Result<RecipeAdaptationResponse>.Unauthorized("Access denied.");

        var aiRecipe = await BuildAiRecipeAsync(recipe, cancellationToken);

        try
        {
            var result = await _recipeAssistantService.AdaptRecipeAsync(aiRecipe, constraints, cancellationToken);
            return Result<RecipeAdaptationResponse>.Success(result.Adapt<RecipeAdaptationResponse>());
        }
        catch (RecipeAssistantInvalidResponseException ex)
        {
            return Result<RecipeAdaptationResponse>.UnprocessableEntity(ex.Message, "recipe_assistant_invalid_response");
        }
        catch (RecipeAssistantUnavailableException ex)
        {
            return Result<RecipeAdaptationResponse>.ServiceUnavailable(ex.Message, "recipe_assistant_unavailable");
        }
    }

    private async Task<Recipe> BuildAiRecipeAsync(
        Recipe recipe,
        CancellationToken cancellationToken)
    {
        recipe.Ingredients = await _recipeIngredientRepository.GetAllByRecipeIdAsync(recipe.Id, cancellationToken);
        recipe.Steps = (await _recipeStepRepository.GetAllByRecipeIdAsync(recipe.Id, cancellationToken))
            .OrderBy(s => s.Order)
            .ToList();
        return recipe;
    }
}
