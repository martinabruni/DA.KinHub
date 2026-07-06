using Kin.KinHub.KinList.Api.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.Shared.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.KinList.Api.KinListFeature;

[ApiController]
[Route("api/audio-operations")]
[Authorize(Policy = FamilyContextRequirement.PolicyName)]
public sealed class AudioOperationsController : ControllerBase
{
    private readonly IKinListService _service;
    private readonly IRequestValidator<CreateAudioProcessingOperationRequest> _createValidator;
    private readonly ICurrentUser _currentUser;

    public AudioOperationsController(
        IKinListService service,
        IRequestValidator<CreateAudioProcessingOperationRequest> createValidator,
        ICurrentUser currentUser)
    {
        _service = service;
        _createValidator = createValidator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAudioProcessingOperationRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ApiProblemDetails.InvalidRequestBody(this);
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.Validation(this, validation.Errors);
        }

        var result = await _service.CreateAudioOperationAsync(request, _currentUser.FamilyId, _currentUser.UserId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return KinListHttpResultMapper.ToActionResult(this, result);
        }

        Response.Headers.Location = $"/api/audio-operations/{result.Value.Id:D}";
        Response.Headers.RetryAfter = result.Value.RetryAfterSeconds.ToString();
        return Accepted(result.Value);
    }

    [HttpPost("{id:guid}/complete-upload")]
    public async Task<IActionResult> CompleteUploadAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.CompleteAudioOperationUploadAsync(id, _currentUser.FamilyId, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Response.Headers.RetryAfter = result.Value.RetryAfterSeconds.ToString();
        }

        return KinListHttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetAudioOperationAsync(id, _currentUser.FamilyId, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Response.Headers.RetryAfter = result.Value.RetryAfterSeconds.ToString();
        }

        return KinListHttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAudioOperationAsync(id, _currentUser.FamilyId, cancellationToken);
        if (!result.IsSuccess)
        {
            return KinListHttpResultMapper.ToActionResult(this, result);
        }

        return NoContent();
    }

}
