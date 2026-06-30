using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.FamilyFeature;

[ApiController]
[Route("api/families")]
public sealed class FamilyController : ControllerBase
{
    private readonly IFamilyService _familyService;
    private readonly IRequestValidator<CreateFamilyRequest> _createValidator;
    private readonly IRequestValidator<AddFamilyMemberRequest> _addMemberValidator;
    private readonly IRequestValidator<UpdateFamilyMemberRequest> _updateMemberValidator;
    private readonly IRequestValidator<UpdateFamilyRequest> _updateFamilyValidator;
    private readonly ICurrentUser _currentUser;

    public FamilyController(
        IFamilyService familyService,
        IRequestValidator<CreateFamilyRequest> createValidator,
        IRequestValidator<AddFamilyMemberRequest> addMemberValidator,
        IRequestValidator<UpdateFamilyMemberRequest> updateMemberValidator,
        IRequestValidator<UpdateFamilyRequest> updateFamilyValidator,
        ICurrentUser currentUser)
    {
        _familyService = familyService;
        _createValidator = createValidator;
        _addMemberValidator = addMemberValidator;
        _updateMemberValidator = updateMemberValidator;
        _updateFamilyValidator = updateFamilyValidator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateFamilyRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _familyService.CreateFamilyAsync(request, _currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToCreatedActionResult(this, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _familyService.GetFamilyAsync(_currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPost("{familyId:guid}/members")]
    public async Task<IActionResult> AddMemberAsync(
        Guid familyId,
        [FromBody] AddFamilyMemberRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _addMemberValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _familyService.AddFamilyMemberAsync(familyId, request, _currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToCreatedActionResult(this, result);
    }

    [HttpDelete("{familyId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> DeleteMemberAsync(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _familyService.DeleteFamilyMemberAsync(familyId, memberId, _currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPut("{familyId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> UpdateMemberAsync(
        Guid familyId,
        Guid memberId,
        [FromBody] UpdateFamilyMemberRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _updateMemberValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _familyService.UpdateFamilyMemberAsync(familyId, memberId, request, _currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPatch("{familyId:guid}")]
    public async Task<IActionResult> UpdateFamilyAsync(
        Guid familyId,
        [FromBody] UpdateFamilyRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _updateFamilyValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _familyService.UpdateFamilyAsync(familyId, request, _currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("{familyId:guid}")]
    public async Task<IActionResult> DeleteFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _familyService.DeleteFamilyAsync(familyId, _currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }
}
