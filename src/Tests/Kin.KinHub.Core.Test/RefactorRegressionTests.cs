using Kin.KinHub.KinRecipe.Business.RecipeAssistantFeature;
using Kin.KinHub.KinRecipe.Domain.RecipeAssistantFeature;
using Kin.KinHub.Core.Domain.FamilyFeature;
using Kin.KinHub.KinRecipe.Domain.RecipeFeature;
using Kin.KinHub.KinList.Business.KinListFeature;
using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.Core.Test;

public sealed class RefactorRegressionTests
{
    [Fact]
    public async Task ParseRecipeAsync_WhenAssistantReturnsInvalidResponse_ReturnsUnprocessableEntity()
    {
        var manager = CreateRecipeAssistantManager(new ThrowingRecipeAssistantService(
            new RecipeAssistantInvalidResponseException("invalid llm payload")));

        var result = await manager.ParseRecipeAsync("make me pasta");

        Assert.Equal(ResultStatus.UnprocessableEntity, result.Status);
        Assert.Equal("recipe_assistant_invalid_response", result.Code);
    }

    [Fact]
    public async Task ParseRecipeAsync_WhenAssistantUnavailable_ReturnsServiceUnavailable()
    {
        var manager = CreateRecipeAssistantManager(new ThrowingRecipeAssistantService(
            new RecipeAssistantUnavailableException("llm unavailable")));

        var result = await manager.ParseRecipeAsync("make me pasta");

        Assert.Equal(ResultStatus.ServiceUnavailable, result.Status);
        Assert.Equal("recipe_assistant_unavailable", result.Code);
    }

    [Fact]
    public void KinListItemDeduplicator_WhenItemAlreadyExists_MarksProposalAsDuplicate()
    {
        var deduplicator = new KinListItemDeduplicator();
        var existingItemId = Guid.NewGuid();

        var result = deduplicator.Deduplicate(
            [" Milk ", "Bread"],
            [
                new DomainKinListItem
                {
                    Id = existingItemId,
                    ListId = Guid.NewGuid(),
                    Text = "milk",
                    Version = Guid.NewGuid(),
                    IsCompleted = false,
                    ActivationOrder = 1,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ]);

        Assert.Collection(
            result.Proposals,
            first =>
            {
                Assert.Equal(" Milk ", first.Text);
                Assert.False(first.IsSelectedByDefault);
                Assert.Equal(existingItemId, first.DuplicateOfItemId);
            },
            second =>
            {
                Assert.Equal("Bread", second.Text);
                Assert.True(second.IsSelectedByDefault);
                Assert.Null(second.DuplicateOfItemId);
            });

        var duplicate = Assert.Single(result.ExistingDuplicates);
        Assert.Equal(existingItemId, duplicate.ItemId);
        Assert.Equal("milk", duplicate.Text);
    }

    private static KinHubRecipeAssistantManager CreateRecipeAssistantManager(IRecipeAssistantService recipeAssistantService) =>
        new(
            new StubFamilyRepository(),
            new StubFridgeRepository(),
            new StubFridgeIngredientRepository(),
            new StubRecipeBookRepository(),
            new StubRecipeRepository(),
            new StubRecipeIngredientRepository(),
            new StubRecipeStepRepository(),
            recipeAssistantService);

    private sealed class ThrowingRecipeAssistantService : IRecipeAssistantService
    {
        private readonly Exception _exception;

        public ThrowingRecipeAssistantService(Exception exception)
        {
            _exception = exception;
        }

        public Task<IReadOnlyList<RecipeSuggestion>> SuggestNewRecipesAsync(IReadOnlyList<RecipeIngredient> fridgeIngredients, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<RecipeSuggestion>>(_exception);

        public Task<Recipe?> ParseRecipeAsync(string rawText, CancellationToken cancellationToken = default) =>
            Task.FromException<Recipe?>(_exception);

        public Task<RecipeAdaptationResult> AdaptRecipeAsync(Recipe recipe, IReadOnlyList<string> constraints, CancellationToken cancellationToken = default) =>
            Task.FromException<RecipeAdaptationResult>(_exception);
    }

    private sealed class StubFamilyRepository : IFamilyRepository
    {
        public Task<Family> CreateAsync(Family model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Family>> CreateRangeAsync(IReadOnlyCollection<Family> models, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Family?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<Family?>(null);
        public Task<Family> GetAsync(Guid key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Family>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Family>>([]);
        public Task<Family> UpdateAsync(Guid key, Family model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Family> DeleteAsync(Guid key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubFridgeRepository : IFridgeRepository
    {
        public Task<Fridge> AddAsync(Fridge fridge, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Fridge>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Fridge>>([]);
        public Task<Fridge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Fridge?>(null);
        public Task<Fridge> UpdateAsync(Fridge fridge, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubFridgeIngredientRepository : IFridgeIngredientRepository
    {
        public Task<FridgeIngredient> AddAsync(FridgeIngredient ingredient, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FridgeIngredient>> GetAllByFridgeIdAsync(Guid fridgeId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FridgeIngredient>>([]);
        public Task<FridgeIngredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<FridgeIngredient?>(null);
        public Task<FridgeIngredient> UpdateAsync(FridgeIngredient ingredient, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubRecipeBookRepository : IRecipeBookRepository
    {
        public Task<RecipeBook> AddAsync(RecipeBook recipeBook, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RecipeBook>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RecipeBook>>([]);
        public Task<RecipeBook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<RecipeBook?>(null);
        public Task<RecipeBook> UpdateAsync(RecipeBook recipeBook, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubRecipeRepository : IRecipeRepository
    {
        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Recipe>> GetAllByRecipeBookIdAsync(Guid recipeBookId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Recipe>>([]);
        public Task<IReadOnlyList<Recipe>> GetAllByRecipeBookIdsAsync(IReadOnlyCollection<Guid> recipeBookIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Recipe>>([]);
        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Recipe?>(null);
        public Task<Recipe> UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubRecipeIngredientRepository : IRecipeIngredientRepository
    {
        public Task<RecipeIngredient> AddAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RecipeIngredient>> AddRangeAsync(IReadOnlyCollection<RecipeIngredient> ingredients, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RecipeIngredient>> GetAllByRecipeIdAsync(Guid recipeId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RecipeIngredient>>([]);
        public Task<IReadOnlyList<RecipeIngredient>> GetAllByRecipeIdsAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RecipeIngredient>>([]);
        public Task<RecipeIngredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<RecipeIngredient?>(null);
        public Task<RecipeIngredient> UpdateAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubRecipeStepRepository : IRecipeStepRepository
    {
        public Task<RecipeStep> AddAsync(RecipeStep step, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RecipeStep>> AddRangeAsync(IReadOnlyCollection<RecipeStep> steps, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RecipeStep>> GetAllByRecipeIdAsync(Guid recipeId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RecipeStep>>([]);
        public Task<IReadOnlyList<RecipeStep>> GetAllByRecipeIdsAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RecipeStep>>([]);
        public Task<RecipeStep?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<RecipeStep?>(null);
        public Task<RecipeStep> UpdateAsync(RecipeStep step, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
