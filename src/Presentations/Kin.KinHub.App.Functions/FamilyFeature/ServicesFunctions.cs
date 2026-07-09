using Kin.KinHub.App.Functions.Common;

namespace Kin.KinHub.App.Functions.FamilyFeature;

public sealed class ServicesFunctions : FunctionsTriggerBase
{
    private readonly FunctionsAuthorizationService _authorizationService;
    private readonly IKinHubServiceService _serviceService;
    private readonly IRequestValidator<ToggleFamilyServiceRequest> _toggleValidator;

    public ServicesFunctions(
        FunctionsAuthorizationService authorizationService,
        IKinHubServiceService serviceService,
        IRequestValidator<ToggleFamilyServiceRequest> toggleValidator)
    {
        _authorizationService = authorizationService;
        _serviceService = serviceService;
        _toggleValidator = toggleValidator;
    }

    [Function(nameof(GetAllAsync))]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/services")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _serviceService.GetAllServicesAsync(cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(GetFamilyServicesAsync))]
    public async Task<IActionResult> GetFamilyServicesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/services/family/{familyId:guid}")] HttpRequest request,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _serviceService.GetFamilyServicesAsync(familyId, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(ToggleAsync))]
    public async Task<IActionResult> ToggleAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/services/family/{familyId:guid}/toggle")] HttpRequest request,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _toggleValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _serviceService.ToggleFamilyServiceAsync(familyId, body, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }
}
