using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

[McpServerToolType]
public sealed class RecipeMcpTools : McpToolBase
{
    private readonly IRecipeBookService _recipeBookService;
    private readonly IRecipeService _recipeService;
    private readonly IRecipeIngredientService _recipeIngredientService;
    private readonly IRecipeStepService _recipeStepService;
    private readonly IFridgeService _fridgeService;
    private readonly IFridgeIngredientService _fridgeIngredientService;
    private readonly IShoppingListService _shoppingListService;
    private readonly IShoppingListItemService _shoppingListItemService;
    private readonly IRecipeMissingIngredientsService _recipeMissingIngredientsService;
    private readonly IRequestValidator<CreateRecipeBookRequest> _createRecipeBookValidator;
    private readonly IRequestValidator<UpdateRecipeBookRequest> _updateRecipeBookValidator;
    private readonly IRequestValidator<CreateRecipeRequest> _createRecipeValidator;
    private readonly IRequestValidator<UpdateRecipeRequest> _updateRecipeValidator;
    private readonly IRequestValidator<CreateRecipeIngredientRequest> _createRecipeIngredientValidator;
    private readonly IRequestValidator<UpdateRecipeIngredientRequest> _updateRecipeIngredientValidator;
    private readonly IRequestValidator<CreateRecipeStepRequest> _createRecipeStepValidator;
    private readonly IRequestValidator<UpdateRecipeStepRequest> _updateRecipeStepValidator;
    private readonly IRequestValidator<CreateFridgeRequest> _createFridgeValidator;
    private readonly IRequestValidator<UpdateFridgeRequest> _updateFridgeValidator;
    private readonly IRequestValidator<CreateFridgeIngredientRequest> _createFridgeIngredientValidator;
    private readonly IRequestValidator<UpdateFridgeIngredientRequest> _updateFridgeIngredientValidator;
    private readonly IRequestValidator<CreateShoppingListRequest> _createShoppingListValidator;
    private readonly IRequestValidator<UpdateShoppingListRequest> _updateShoppingListValidator;
    private readonly IRequestValidator<CreateShoppingListItemRequest> _createShoppingListItemValidator;
    private readonly IRequestValidator<BulkAddShoppingListItemsRequest> _bulkAddShoppingListItemsValidator;

    public RecipeMcpTools(
        ICurrentUser currentUser,
        IRecipeBookService recipeBookService,
        IRecipeService recipeService,
        IRecipeIngredientService recipeIngredientService,
        IRecipeStepService recipeStepService,
        IFridgeService fridgeService,
        IFridgeIngredientService fridgeIngredientService,
        IShoppingListService shoppingListService,
        IShoppingListItemService shoppingListItemService,
        IRecipeMissingIngredientsService recipeMissingIngredientsService,
        IRequestValidator<CreateRecipeBookRequest> createRecipeBookValidator,
        IRequestValidator<UpdateRecipeBookRequest> updateRecipeBookValidator,
        IRequestValidator<CreateRecipeRequest> createRecipeValidator,
        IRequestValidator<UpdateRecipeRequest> updateRecipeValidator,
        IRequestValidator<CreateRecipeIngredientRequest> createRecipeIngredientValidator,
        IRequestValidator<UpdateRecipeIngredientRequest> updateRecipeIngredientValidator,
        IRequestValidator<CreateRecipeStepRequest> createRecipeStepValidator,
        IRequestValidator<UpdateRecipeStepRequest> updateRecipeStepValidator,
        IRequestValidator<CreateFridgeRequest> createFridgeValidator,
        IRequestValidator<UpdateFridgeRequest> updateFridgeValidator,
        IRequestValidator<CreateFridgeIngredientRequest> createFridgeIngredientValidator,
        IRequestValidator<UpdateFridgeIngredientRequest> updateFridgeIngredientValidator,
        IRequestValidator<CreateShoppingListRequest> createShoppingListValidator,
        IRequestValidator<UpdateShoppingListRequest> updateShoppingListValidator,
        IRequestValidator<CreateShoppingListItemRequest> createShoppingListItemValidator,
        IRequestValidator<BulkAddShoppingListItemsRequest> bulkAddShoppingListItemsValidator)
        : base(currentUser)
    {
        _recipeBookService = recipeBookService;
        _recipeService = recipeService;
        _recipeIngredientService = recipeIngredientService;
        _recipeStepService = recipeStepService;
        _fridgeService = fridgeService;
        _fridgeIngredientService = fridgeIngredientService;
        _shoppingListService = shoppingListService;
        _shoppingListItemService = shoppingListItemService;
        _recipeMissingIngredientsService = recipeMissingIngredientsService;
        _createRecipeBookValidator = createRecipeBookValidator;
        _updateRecipeBookValidator = updateRecipeBookValidator;
        _createRecipeValidator = createRecipeValidator;
        _updateRecipeValidator = updateRecipeValidator;
        _createRecipeIngredientValidator = createRecipeIngredientValidator;
        _updateRecipeIngredientValidator = updateRecipeIngredientValidator;
        _createRecipeStepValidator = createRecipeStepValidator;
        _updateRecipeStepValidator = updateRecipeStepValidator;
        _createFridgeValidator = createFridgeValidator;
        _updateFridgeValidator = updateFridgeValidator;
        _createFridgeIngredientValidator = createFridgeIngredientValidator;
        _updateFridgeIngredientValidator = updateFridgeIngredientValidator;
        _createShoppingListValidator = createShoppingListValidator;
        _updateShoppingListValidator = updateShoppingListValidator;
        _createShoppingListItemValidator = createShoppingListItemValidator;
        _bulkAddShoppingListItemsValidator = bulkAddShoppingListItemsValidator;
    }

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "recipe-book.create"), Description("Create a recipe book.")]
    public Task<CallToolResult> CreateRecipeBookAsync(
        [Description("The recipe book creation payload.")] CreateRecipeBookRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _createRecipeBookValidator,
            async (payload, ct) => await _recipeBookService.CreateAsync(payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "recipe-book.list"), Description("List recipe books.")]
    public async Task<CallToolResult> ListRecipeBooksAsync(CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeBookService.GetAllAsync(CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "recipe-book.get"), Description("Get a recipe book.")]
    public async Task<CallToolResult> GetRecipeBookAsync(
        [Description("The target recipe book id.")] Guid id,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeBookService.GetByIdAsync(id, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "recipe-book.update"), Description("Update a recipe book.")]
    public Task<CallToolResult> UpdateRecipeBookAsync(
        [Description("The target recipe book id.")] Guid id,
        [Description("The recipe book update payload.")] UpdateRecipeBookRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _updateRecipeBookValidator,
            async (payload, ct) => await _recipeBookService.UpdateAsync(id, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Admin)]
    [McpServerTool(Name = "recipe-book.delete"), Description("Delete a recipe book.")]
    public async Task<CallToolResult> DeleteRecipeBookAsync(
        [Description("The target recipe book id.")] Guid id,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeBookService.DeleteAsync(id, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "recipe.create"), Description("Create a recipe.")]
    public Task<CallToolResult> CreateRecipeAsync(
        [Description("The recipe creation payload.")] CreateRecipeRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _createRecipeValidator,
            async (payload, ct) => await _recipeService.CreateAsync(payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "recipe.list"), Description("List recipes for a recipe book.")]
    public async Task<CallToolResult> ListRecipesAsync(
        [Description("The target recipe book id.")] Guid recipeBookId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeService.GetAllAsync(recipeBookId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "recipe.get"), Description("Get a recipe.")]
    public async Task<CallToolResult> GetRecipeAsync(
        [Description("The target recipe id.")] Guid recipeId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeService.GetByIdAsync(recipeId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "recipe.update"), Description("Update a recipe.")]
    public Task<CallToolResult> UpdateRecipeAsync(
        [Description("The target recipe id.")] Guid recipeId,
        [Description("The recipe update payload.")] UpdateRecipeRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _updateRecipeValidator,
            async (payload, ct) => await _recipeService.UpdateAsync(recipeId, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Admin)]
    [McpServerTool(Name = "recipe.delete"), Description("Delete a recipe.")]
    public async Task<CallToolResult> DeleteRecipeAsync(
        [Description("The target recipe id.")] Guid recipeId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeService.DeleteAsync(recipeId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "recipe.missing-ingredients"), Description("Compute missing ingredients for a recipe and fridge.")]
    public async Task<CallToolResult> GetMissingIngredientsAsync(
        [Description("The target recipe id.")] Guid recipeId,
        [Description("The target fridge id.")] Guid fridgeId,
        CancellationToken cancellationToken = default)
    {
        var recipeResult = await _recipeService.GetByIdAsync(recipeId, CurrentUser.UserId, cancellationToken);
        if (!recipeResult.IsSuccess)
            return McpErrorMapper.FromCoreResult(recipeResult);

        var fridgeResult = await _fridgeService.GetByIdAsync(fridgeId, CurrentUser.UserId, cancellationToken);
        if (!fridgeResult.IsSuccess)
            return McpErrorMapper.FromCoreResult(fridgeResult);

        return McpErrorMapper.ToolSuccess(new
        {
            missingIngredients = await _recipeMissingIngredientsService.GetMissingIngredientsAsync(recipeId, fridgeId, cancellationToken),
        });
    }

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "recipe.ingredient.create"), Description("Create a recipe ingredient.")]
    public Task<CallToolResult> CreateRecipeIngredientAsync(
        [Description("The recipe ingredient creation payload.")] CreateRecipeIngredientRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _createRecipeIngredientValidator,
            async (payload, ct) => await _recipeIngredientService.CreateAsync(payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "recipe.ingredient.list"), Description("List recipe ingredients.")]
    public async Task<CallToolResult> ListRecipeIngredientsAsync(
        [Description("The target recipe id.")] Guid recipeId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeIngredientService.GetAllAsync(recipeId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "recipe.ingredient.get"), Description("Get a recipe ingredient.")]
    public async Task<CallToolResult> GetRecipeIngredientAsync(
        [Description("The target ingredient id.")] Guid ingredientId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeIngredientService.GetByIdAsync(ingredientId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "recipe.ingredient.update"), Description("Update a recipe ingredient.")]
    public Task<CallToolResult> UpdateRecipeIngredientAsync(
        [Description("The target ingredient id.")] Guid ingredientId,
        [Description("The recipe ingredient update payload.")] UpdateRecipeIngredientRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _updateRecipeIngredientValidator,
            async (payload, ct) => await _recipeIngredientService.UpdateAsync(ingredientId, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Admin)]
    [McpServerTool(Name = "recipe.ingredient.delete"), Description("Delete a recipe ingredient.")]
    public async Task<CallToolResult> DeleteRecipeIngredientAsync(
        [Description("The target ingredient id.")] Guid ingredientId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeIngredientService.DeleteAsync(ingredientId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "recipe.step.create"), Description("Create a recipe step.")]
    public Task<CallToolResult> CreateRecipeStepAsync(
        [Description("The recipe step creation payload.")] CreateRecipeStepRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _createRecipeStepValidator,
            async (payload, ct) => await _recipeStepService.CreateAsync(payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "recipe.step.list"), Description("List recipe steps.")]
    public async Task<CallToolResult> ListRecipeStepsAsync(
        [Description("The target recipe id.")] Guid recipeId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeStepService.GetAllAsync(recipeId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "recipe.step.get"), Description("Get a recipe step.")]
    public async Task<CallToolResult> GetRecipeStepAsync(
        [Description("The target step id.")] Guid stepId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeStepService.GetByIdAsync(stepId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "recipe.step.update"), Description("Update a recipe step.")]
    public Task<CallToolResult> UpdateRecipeStepAsync(
        [Description("The target step id.")] Guid stepId,
        [Description("The recipe step update payload.")] UpdateRecipeStepRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _updateRecipeStepValidator,
            async (payload, ct) => await _recipeStepService.UpdateAsync(stepId, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Admin)]
    [McpServerTool(Name = "recipe.step.delete"), Description("Delete a recipe step.")]
    public async Task<CallToolResult> DeleteRecipeStepAsync(
        [Description("The target step id.")] Guid stepId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _recipeStepService.DeleteAsync(stepId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "fridge.create"), Description("Create a fridge.")]
    public Task<CallToolResult> CreateFridgeAsync(
        [Description("The fridge creation payload.")] CreateFridgeRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _createFridgeValidator,
            async (payload, ct) => await _fridgeService.CreateAsync(payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "fridge.list"), Description("List fridges.")]
    public async Task<CallToolResult> ListFridgesAsync(CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _fridgeService.GetAllAsync(CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "fridge.get"), Description("Get a fridge.")]
    public async Task<CallToolResult> GetFridgeAsync(
        [Description("The target fridge id.")] Guid id,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _fridgeService.GetByIdAsync(id, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "fridge.update"), Description("Update a fridge.")]
    public Task<CallToolResult> UpdateFridgeAsync(
        [Description("The target fridge id.")] Guid id,
        [Description("The fridge update payload.")] UpdateFridgeRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _updateFridgeValidator,
            async (payload, ct) => await _fridgeService.UpdateAsync(id, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Admin)]
    [McpServerTool(Name = "fridge.delete"), Description("Delete a fridge.")]
    public async Task<CallToolResult> DeleteFridgeAsync(
        [Description("The target fridge id.")] Guid id,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _fridgeService.DeleteAsync(id, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "fridge.ingredient.create"), Description("Create a fridge ingredient.")]
    public Task<CallToolResult> CreateFridgeIngredientAsync(
        [Description("The fridge ingredient creation payload.")] CreateFridgeIngredientRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _createFridgeIngredientValidator,
            async (payload, ct) => await _fridgeIngredientService.CreateAsync(payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "fridge.ingredient.list"), Description("List fridge ingredients.")]
    public async Task<CallToolResult> ListFridgeIngredientsAsync(
        [Description("The target fridge id.")] Guid fridgeId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _fridgeIngredientService.GetAllAsync(fridgeId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "fridge.ingredient.get"), Description("Get a fridge ingredient.")]
    public async Task<CallToolResult> GetFridgeIngredientAsync(
        [Description("The target ingredient id.")] Guid ingredientId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _fridgeIngredientService.GetByIdAsync(ingredientId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "fridge.ingredient.update"), Description("Update a fridge ingredient.")]
    public Task<CallToolResult> UpdateFridgeIngredientAsync(
        [Description("The target ingredient id.")] Guid ingredientId,
        [Description("The fridge ingredient update payload.")] UpdateFridgeIngredientRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _updateFridgeIngredientValidator,
            async (payload, ct) => await _fridgeIngredientService.UpdateAsync(ingredientId, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Admin)]
    [McpServerTool(Name = "fridge.ingredient.delete"), Description("Delete a fridge ingredient.")]
    public async Task<CallToolResult> DeleteFridgeIngredientAsync(
        [Description("The target ingredient id.")] Guid ingredientId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _fridgeIngredientService.DeleteAsync(ingredientId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "shopping-list.create"), Description("Create a shopping list.")]
    public Task<CallToolResult> CreateShoppingListAsync(
        [Description("The shopping list creation payload.")] CreateShoppingListRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _createShoppingListValidator,
            async (payload, ct) => await _shoppingListService.CreateAsync(payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "shopping-list.list"), Description("List shopping lists.")]
    public async Task<CallToolResult> ListShoppingListsAsync(CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _shoppingListService.GetAllAsync(CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "shopping-list.update"), Description("Update a shopping list.")]
    public Task<CallToolResult> UpdateShoppingListAsync(
        [Description("The target shopping list id.")] Guid id,
        [Description("The shopping list update payload.")] UpdateShoppingListRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _updateShoppingListValidator,
            async (payload, ct) => await _shoppingListService.UpdateAsync(id, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Admin)]
    [McpServerTool(Name = "shopping-list.delete"), Description("Delete a shopping list.")]
    public async Task<CallToolResult> DeleteShoppingListAsync(
        [Description("The target shopping list id.")] Guid id,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _shoppingListService.DeleteAsync(id, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Read)]
    [McpServerTool(Name = "shopping-list.item.list"), Description("List shopping list items.")]
    public async Task<CallToolResult> ListShoppingListItemsAsync(
        [Description("The target shopping list id.")] Guid listId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _shoppingListItemService.GetAllByListIdAsync(listId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "shopping-list.item.add"), Description("Add a shopping list item.")]
    public Task<CallToolResult> AddShoppingListItemAsync(
        [Description("The target shopping list id.")] Guid listId,
        [Description("The shopping list item creation payload.")] CreateShoppingListItemRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (request is not null)
            request.ShoppingListId = listId;

        return ExecuteCoreValidatedAsync(
            request,
            _createShoppingListItemValidator,
            async (payload, ct) => await _shoppingListItemService.AddAsync(listId, payload, CurrentUser.UserId, ct),
            cancellationToken);
    }

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "shopping-list.item.bulk-add"), Description("Bulk add shopping list items.")]
    public Task<CallToolResult> BulkAddShoppingListItemsAsync(
        [Description("The target shopping list id.")] Guid listId,
        [Description("The bulk-add payload.")] BulkAddShoppingListItemsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (request is not null)
            request.ShoppingListId = listId;

        return ExecuteCoreValidatedAsync(
            request,
            _bulkAddShoppingListItemsValidator,
            async (payload, ct) => await _shoppingListItemService.BulkAddAsync(listId, payload, CurrentUser.UserId, ct),
            cancellationToken);
    }

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Write)]
    [McpServerTool(Name = "shopping-list.item.toggle"), Description("Toggle a shopping list item.")]
    public async Task<CallToolResult> ToggleShoppingListItemAsync(
        [Description("The target shopping list id.")] Guid listId,
        [Description("The target shopping list item id.")] Guid itemId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _shoppingListItemService.ToggleCheckedAsync(listId, itemId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Admin)]
    [McpServerTool(Name = "shopping-list.item.delete"), Description("Delete a shopping list item.")]
    public async Task<CallToolResult> DeleteShoppingListItemAsync(
        [Description("The target shopping list id.")] Guid listId,
        [Description("The target shopping list item id.")] Guid itemId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _shoppingListItemService.DeleteAsync(listId, itemId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [Authorize(Policy = McpAuthorizationPolicies.Admin)]
    [McpServerTool(Name = "shopping-list.item.delete-checked"), Description("Delete checked shopping list items.")]
    public async Task<CallToolResult> DeleteCheckedShoppingListItemsAsync(
        [Description("The target shopping list id.")] Guid listId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _shoppingListItemService.DeleteCheckedAsync(listId, CurrentUser.UserId, cancellationToken));
}
