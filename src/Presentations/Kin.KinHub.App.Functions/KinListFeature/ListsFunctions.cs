using Kin.KinHub.App.Functions.Common;

namespace Kin.KinHub.App.Functions.KinListFeature;

public sealed class ListsFunctions : FunctionsTriggerBase
{
    private readonly FunctionsAuthorizationService _authorizationService;
    private readonly IKinListService _service;
    private readonly IRequestValidator<CreateKinListRequest> _createValidator;
    private readonly IRequestValidator<UpdateKinListRequest> _updateValidator;
    private readonly IRequestValidator<CreateKinListItemRequest> _createItemValidator;
    private readonly IRequestValidator<BulkConfirmKinListItemsRequest> _bulkConfirmValidator;
    private readonly IRequestValidator<UpdateKinListItemRequest> _updateItemValidator;

    public ListsFunctions(
        FunctionsAuthorizationService authorizationService,
        IKinListService service,
        IRequestValidator<CreateKinListRequest> createValidator,
        IRequestValidator<UpdateKinListRequest> updateValidator,
        IRequestValidator<CreateKinListItemRequest> createItemValidator,
        IRequestValidator<BulkConfirmKinListItemsRequest> bulkConfirmValidator,
        IRequestValidator<UpdateKinListItemRequest> updateItemValidator)
    {
        _authorizationService = authorizationService;
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _createItemValidator = createItemValidator;
        _bulkConfirmValidator = bulkConfirmValidator;
        _updateItemValidator = updateItemValidator;
    }

    [Function(nameof(GetAllAsync))]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/lists")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _service.GetAllAsync(_authorizationService.CurrentUser.FamilyId, cancellationToken);
        return ToKinListActionResult(request, result);
    }

    [Function(nameof(GetByIdAsync))]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/lists/{id:guid}")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _service.GetByIdAsync(id, _authorizationService.CurrentUser.FamilyId, cancellationToken);
        return ApplyEtag(request, ToKinListActionResult(request, result), result);
    }

    [Function(nameof(CreateAsync))]
    public async Task<IActionResult> CreateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/lists")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var idempotencyKey = request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "idempotency_key_required", "The Idempotency-Key header is required.");
        }

        var (body, error) = await ReadAndValidateAsync(request, _createValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _service.CreateAsync(
            body,
            _authorizationService.CurrentUser.FamilyId,
            _authorizationService.CurrentUser.UserId,
            idempotencyKey.Trim(),
            cancellationToken);
        return ApplyEtag(request, ToKinListCreatedActionResult(request, result), result);
    }

    [Function(nameof(UpdateAsync))]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "api/lists/{id:guid}")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var ifMatch = ReadIfMatch(request);
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "if_match_required", "The If-Match header is required.");
        }

        var (body, error) = await ReadAndValidateAsync(request, _updateValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _service.UpdateAsync(id, body, _authorizationService.CurrentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(request, ToKinListActionResult(request, result), result);
    }

    [Function(nameof(DeleteAsync))]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "api/lists/{id:guid}")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var ifMatch = ReadIfMatch(request);
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "if_match_required", "The If-Match header is required.");
        }

        var result = await _service.DeleteAsync(id, _authorizationService.CurrentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(request, ToKinListActionResult(request, result), result);
    }

    [Function(nameof(RestoreAsync))]
    public async Task<IActionResult> RestoreAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/lists/{id:guid}/restore")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var ifMatch = ReadIfMatch(request);
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "if_match_required", "The If-Match header is required.");
        }

        var result = await _service.RestoreAsync(id, _authorizationService.CurrentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(request, ToKinListActionResult(request, result), result);
    }

    [Function(nameof(AddItemAsync))]
    public async Task<IActionResult> AddItemAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/lists/{id:guid}/items")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var ifMatch = ReadIfMatch(request);
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "if_match_required", "The If-Match header is required.");
        }

        var (body, error) = await ReadAndValidateAsync(request, _createItemValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _service.AddItemAsync(id, body, _authorizationService.CurrentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(request, ToKinListActionResult(request, result), result);
    }

    [Function(nameof(BulkConfirmItemsAsync))]
    public async Task<IActionResult> BulkConfirmItemsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/lists/{id:guid}/items/confirm")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var ifMatch = ReadIfMatch(request);
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "if_match_required", "The If-Match header is required.");
        }

        var (body, error) = await ReadAndValidateAsync(request, _bulkConfirmValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _service.BulkConfirmItemsAsync(id, body, _authorizationService.CurrentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(request, ToKinListActionResult(request, result), result);
    }

    [Function(nameof(UpdateItemAsync))]
    public async Task<IActionResult> UpdateItemAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "api/lists/{id:guid}/items/{itemId:guid}")] HttpRequest request,
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var ifMatch = ReadIfMatch(request);
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "if_match_required", "The If-Match header is required.");
        }

        var (body, error) = await ReadAndValidateAsync(request, _updateItemValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _service.UpdateItemAsync(id, itemId, body, _authorizationService.CurrentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(request, ToKinListActionResult(request, result), result);
    }

    [Function(nameof(DeleteItemAsync))]
    public async Task<IActionResult> DeleteItemAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "api/lists/{id:guid}/items/{itemId:guid}")] HttpRequest request,
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var ifMatch = ReadIfMatch(request);
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "if_match_required", "The If-Match header is required.");
        }

        var result = await _service.DeleteItemAsync(id, itemId, _authorizationService.CurrentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(request, ToKinListActionResult(request, result), result);
    }

    [Function(nameof(RestoreItemAsync))]
    public async Task<IActionResult> RestoreItemAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/lists/{id:guid}/items/{itemId:guid}/restore")] HttpRequest request,
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var ifMatch = ReadIfMatch(request);
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(CreateController(request), "if_match_required", "The If-Match header is required.");
        }

        var result = await _service.RestoreItemAsync(id, itemId, _authorizationService.CurrentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(request, ToKinListActionResult(request, result), result);
    }

    private static IActionResult ApplyEtag(
        HttpRequest request,
        IActionResult actionResult,
        Result<KinListDetailResponse> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            request.HttpContext.Response.Headers.ETag = result.Value.ETag;
        }

        return actionResult;
    }
}
