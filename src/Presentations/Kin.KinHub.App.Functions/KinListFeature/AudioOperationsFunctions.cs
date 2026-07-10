using Kin.KinHub.App.Functions.Common;

namespace Kin.KinHub.App.Functions.KinListFeature;

public sealed class AudioOperationsFunctions : FunctionsTriggerBase
{
    private readonly FunctionsAuthorizationService _authorizationService;
    private readonly IKinListAudioService _service;
    private readonly IRequestValidator<CreateAudioProcessingOperationRequest> _createValidator;

    public AudioOperationsFunctions(
        FunctionsAuthorizationService authorizationService,
        IKinListAudioService service,
        IRequestValidator<CreateAudioProcessingOperationRequest> createValidator)
    {
        _authorizationService = authorizationService;
        _service = service;
        _createValidator = createValidator;
    }

    [Function(nameof(CreateAsync))]
    public async Task<IActionResult> CreateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "audio-operations")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _createValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _service.CreateAudioOperationAsync(
            body,
            _authorizationService.CurrentUser.FamilyId,
            _authorizationService.CurrentUser.UserId,
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ToKinListActionResult(request, result);
        }

        request.HttpContext.Response.Headers.Location = $"/api/audio-operations/{result.Value.Id:D}";
        request.HttpContext.Response.Headers.RetryAfter = result.Value.RetryAfterSeconds.ToString();
        return new ObjectResult(result.Value) { StatusCode = StatusCodes.Status202Accepted };
    }

    [Function(nameof(CompleteUploadAsync))]
    public async Task<IActionResult> CompleteUploadAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "audio-operations/{id:guid}/complete-upload")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _service.CompleteAudioOperationUploadAsync(id, _authorizationService.CurrentUser.FamilyId, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            request.HttpContext.Response.Headers.RetryAfter = result.Value.RetryAfterSeconds.ToString();
        }

        return ToKinListActionResult(request, result);
    }

    [Function(nameof(GetAsync))]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "audio-operations/{id:guid}")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _service.GetAudioOperationAsync(id, _authorizationService.CurrentUser.FamilyId, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            request.HttpContext.Response.Headers.RetryAfter = result.Value.RetryAfterSeconds.ToString();
        }

        return ToKinListActionResult(request, result);
    }

    [Function(nameof(DeleteAsync))]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "audio-operations/{id:guid}")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _service.DeleteAudioOperationAsync(id, _authorizationService.CurrentUser.FamilyId, cancellationToken);
        return result.IsSuccess
            ? new StatusCodeResult(StatusCodes.Status204NoContent)
            : ToKinListActionResult(request, result);
    }
}
