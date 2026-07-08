using Kin.KinHub.App.Functions.Common;

namespace Kin.KinHub.App.Functions.RecipeFeature;

public sealed class FridgeIngredientFunctions : FunctionsTriggerBase
{
    private readonly FunctionsAuthorizationService _authorizationService;
    private readonly IFridgeIngredientService _service;
    private readonly IRequestValidator<CreateFridgeIngredientRequest> _createValidator;
    private readonly IRequestValidator<UpdateFridgeIngredientRequest> _updateValidator;

    public FridgeIngredientFunctions(
        FunctionsAuthorizationService authorizationService,
        IFridgeIngredientService service,
        IRequestValidator<CreateFridgeIngredientRequest> createValidator,
        IRequestValidator<UpdateFridgeIngredientRequest> updateValidator)
    {
        _authorizationService = authorizationService;
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [Function(nameof(CreateAsync))]
    public async Task<IActionResult> CreateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/fridges/{fridgeId:guid}/ingredients")] HttpRequest request,
        Guid fridgeId,
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

        var result = await _service.CreateAsync(body, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToCreatedActionResult(request, result);
    }

    [Function(nameof(GetAllAsync))]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/fridges/{fridgeId:guid}/ingredients")] HttpRequest request,
        Guid fridgeId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _service.GetAllAsync(fridgeId, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(GetByIdAsync))]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/fridges/{fridgeId:guid}/ingredients/{id:guid}")] HttpRequest request,
        Guid fridgeId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _service.GetByIdAsync(id, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(UpdateAsync))]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "api/fridges/{fridgeId:guid}/ingredients/{id:guid}")] HttpRequest request,
        Guid fridgeId,
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

        var result = await _service.UpdateAsync(id, body, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(DeleteAsync))]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "api/fridges/{fridgeId:guid}/ingredients/{id:guid}")] HttpRequest request,
        Guid fridgeId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _service.DeleteAsync(id, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }
}
