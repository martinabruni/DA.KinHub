using Kin.KinHub.Core.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Identity.Api.FamilyFeature;

[ApiController]
[Route("api/services")]
public sealed class ServicesController : ControllerBase
{
    private readonly IKinHubServiceService _serviceService;
    private readonly IRequestValidator<ToggleFamilyServiceRequest> _toggleValidator;
    private readonly ICurrentUser _currentUser;

    public ServicesController(
        IKinHubServiceService serviceService,
        IRequestValidator<ToggleFamilyServiceRequest> toggleValidator,
        ICurrentUser currentUser)
    {
        _serviceService = serviceService;
        _toggleValidator = toggleValidator;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _serviceService.GetAllServicesAsync(cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("family/{familyId:guid}")]
    [Authorize(Policy = FamilyContextRequirement.PolicyName)]
    public async Task<IActionResult> GetFamilyServicesAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var result = await _serviceService.GetFamilyServicesAsync(familyId, _currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPost("family/{familyId:guid}/toggle")]
    [Authorize(Policy = FamilyContextRequirement.PolicyName)]
    public async Task<IActionResult> ToggleAsync(
        Guid familyId,
        [FromBody] ToggleFamilyServiceRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _toggleValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _serviceService.ToggleFamilyServiceAsync(familyId, request, _currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }
}
