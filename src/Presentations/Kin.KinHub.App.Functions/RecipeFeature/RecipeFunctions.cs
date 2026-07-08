using Kin.KinHub.App.Functions.Common;

namespace Kin.KinHub.App.Functions.RecipeFeature;

public sealed class RecipeFunctions : FunctionsTriggerBase
{
    private readonly FunctionsAuthorizationService _authorizationService;
    private readonly IRecipeService _recipeService;
    private readonly IFridgeService _fridgeService;
    private readonly IRecipeMissingIngredientsService _missingIngredientsService;
    private readonly IRequestValidator<CreateRecipeRequest> _createValidator;
    private readonly IRequestValidator<UpdateRecipeRequest> _updateValidator;

    public RecipeFunctions(
        FunctionsAuthorizationService authorizationService,
        IRecipeService recipeService,
        IFridgeService fridgeService,
        IRecipeMissingIngredientsService missingIngredientsService,
        IRequestValidator<CreateRecipeRequest> createValidator,
        IRequestValidator<UpdateRecipeRequest> updateValidator)
    {
        _authorizationService = authorizationService;
        _recipeService = recipeService;
        _fridgeService = fridgeService;
        _missingIngredientsService = missingIngredientsService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [Function(nameof(CreateAsync))]
    public async Task<IActionResult> CreateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/recipe-books/{recipeBookId:guid}/recipes")] HttpRequest request,
        Guid recipeBookId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _createValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _recipeService.CreateAsync(body, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToCreatedActionResult(request, result);
    }

    [Function(nameof(GetAllAsync))]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/recipe-books/{recipeBookId:guid}/recipes")] HttpRequest request,
        Guid recipeBookId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _recipeService.GetAllAsync(recipeBookId, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(GetByIdAsync))]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/recipe-books/{recipeBookId:guid}/recipes/{id:guid}")] HttpRequest request,
        Guid recipeBookId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _recipeService.GetByIdAsync(id, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(UpdateAsync))]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "api/recipe-books/{recipeBookId:guid}/recipes/{id:guid}")] HttpRequest request,
        Guid recipeBookId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _updateValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _recipeService.UpdateAsync(id, body, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(DeleteAsync))]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "api/recipe-books/{recipeBookId:guid}/recipes/{id:guid}")] HttpRequest request,
        Guid recipeBookId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _recipeService.DeleteAsync(id, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(GetMissingIngredientsAsync))]
    public async Task<IActionResult> GetMissingIngredientsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/recipe-books/{recipeBookId:guid}/recipes/{id:guid}/missing-ingredients")] HttpRequest request,
        Guid recipeBookId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (!Guid.TryParse(request.Query["fridgeId"], out var fridgeId))
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "validation_error", "The request is invalid.");
        }

        var recipeResult = await _recipeService.GetByIdAsync(id, _authorizationService.CurrentUser.UserId, cancellationToken);
        if (!recipeResult.IsSuccess)
        {
            return ToActionResult(request, recipeResult);
        }

        var fridgeResult = await _fridgeService.GetByIdAsync(fridgeId, _authorizationService.CurrentUser.UserId, cancellationToken);
        if (!fridgeResult.IsSuccess)
        {
            return ToActionResult(request, fridgeResult);
        }

        var missing = await _missingIngredientsService.GetMissingIngredientsAsync(id, fridgeId, cancellationToken);
        return new OkObjectResult(new { missingIngredients = missing });
    }
}
