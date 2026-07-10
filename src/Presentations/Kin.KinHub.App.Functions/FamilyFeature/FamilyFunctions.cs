using Kin.KinHub.App.Functions.Common;

namespace Kin.KinHub.App.Functions.FamilyFeature;

public sealed class FamilyFunctions : FunctionsTriggerBase
{
    private readonly FunctionsAuthorizationService _authorizationService;
    private readonly IFamilyService _familyService;
    private readonly IRequestValidator<CreateFamilyRequest> _createValidator;
    private readonly IRequestValidator<AddFamilyMemberRequest> _addMemberValidator;
    private readonly IRequestValidator<UpdateFamilyMemberRequest> _updateMemberValidator;
    private readonly IRequestValidator<UpdateFamilyRequest> _updateFamilyValidator;

    public FamilyFunctions(
        FunctionsAuthorizationService authorizationService,
        IFamilyService familyService,
        IRequestValidator<CreateFamilyRequest> createValidator,
        IRequestValidator<AddFamilyMemberRequest> addMemberValidator,
        IRequestValidator<UpdateFamilyMemberRequest> updateMemberValidator,
        IRequestValidator<UpdateFamilyRequest> updateFamilyValidator)
    {
        _authorizationService = authorizationService;
        _familyService = familyService;
        _createValidator = createValidator;
        _addMemberValidator = addMemberValidator;
        _updateMemberValidator = updateMemberValidator;
        _updateFamilyValidator = updateFamilyValidator;
    }

    [Function(nameof(CreateAsync))]
    public async Task<IActionResult> CreateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families")] HttpRequest request,
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

        var result = await _familyService.CreateFamilyAsync(body, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToCreatedActionResult(request, result);
    }

    [Function(nameof(GetAsync))]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "families")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _familyService.GetFamilyAsync(_authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(AddMemberAsync))]
    public async Task<IActionResult> AddMemberAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families/{familyId:guid}/members")] HttpRequest request,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _addMemberValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _familyService.AddFamilyMemberAsync(familyId, body, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToCreatedActionResult(request, result);
    }

    [Function(nameof(DeleteMemberAsync))]
    public async Task<IActionResult> DeleteMemberAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "families/{familyId:guid}/members/{memberId:guid}")] HttpRequest request,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _familyService.DeleteFamilyMemberAsync(familyId, memberId, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(UpdateMemberAsync))]
    public async Task<IActionResult> UpdateMemberAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "families/{familyId:guid}/members/{memberId:guid}")] HttpRequest request,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _updateMemberValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _familyService.UpdateFamilyMemberAsync(familyId, memberId, body, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(UpdateFamilyAsync))]
    public async Task<IActionResult> UpdateFamilyAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "families/{familyId:guid}")] HttpRequest request,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _updateFamilyValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _familyService.UpdateFamilyAsync(familyId, body, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(DeleteFamilyAsync))]
    public async Task<IActionResult> DeleteFamilyAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "families/{familyId:guid}")] HttpRequest request,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureFamilyContextAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await _familyService.DeleteFamilyAsync(familyId, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }
}
