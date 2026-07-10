using Kin.KinHub.App.Functions.Common;

namespace Kin.KinHub.App.Functions.RecipeFeature;

public sealed class RecipeBookFunctions : FunctionsTriggerBase
{
    private readonly FunctionsAuthorizationService _authorizationService;
    private readonly IRecipeBookService _service;
    private readonly IRequestValidator<CreateRecipeBookRequest> _createValidator;
    private readonly IRequestValidator<UpdateRecipeBookRequest> _updateValidator;

    public RecipeBookFunctions(
        FunctionsAuthorizationService authorizationService,
        IRecipeBookService service,
        IRequestValidator<CreateRecipeBookRequest> createValidator,
        IRequestValidator<UpdateRecipeBookRequest> updateValidator)
    {
        _authorizationService = authorizationService;
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [Function(nameof(CreateAsync))]
    public async Task<IActionResult> CreateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "recipe-books")] HttpRequest request,
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "recipe-books")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _service.GetAllAsync(_authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(GetByIdAsync))]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "recipe-books/{id:guid}")] HttpRequest request,
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "recipe-books/{id:guid}")] HttpRequest request,
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "recipe-books/{id:guid}")] HttpRequest request,
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
