using Kin.KinHub.KinList.Api.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.Shared.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.KinList.Api.KinListFeature;

[ApiController]
[Route("api/lists")]
[Authorize(Policy = FamilyContextRequirement.PolicyName)]
public sealed class ListsController : ControllerBase
{
    private readonly IKinListService _service;
    private readonly IRequestValidator<CreateKinListRequest> _createValidator;
    private readonly IRequestValidator<UpdateKinListRequest> _updateValidator;
    private readonly IRequestValidator<CreateKinListItemRequest> _createItemValidator;
    private readonly IRequestValidator<BulkConfirmKinListItemsRequest> _bulkConfirmValidator;
    private readonly IRequestValidator<UpdateKinListItemRequest> _updateItemValidator;
    private readonly IRequestValidator<KinListAudioFormRequest> _audioValidator;
    private readonly ICurrentUser _currentUser;

    public ListsController(
        IKinListService service,
        IRequestValidator<CreateKinListRequest> createValidator,
        IRequestValidator<UpdateKinListRequest> updateValidator,
        IRequestValidator<CreateKinListItemRequest> createItemValidator,
        IRequestValidator<BulkConfirmKinListItemsRequest> bulkConfirmValidator,
        IRequestValidator<UpdateKinListItemRequest> updateItemValidator,
        IRequestValidator<KinListAudioFormRequest> audioValidator,
        ICurrentUser currentUser)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _createItemValidator = createItemValidator;
        _bulkConfirmValidator = bulkConfirmValidator;
        _updateItemValidator = updateItemValidator;
        _audioValidator = audioValidator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        var result = await _service.GetAllAsync(_currentUser.FamilyId, cancellationToken);
        return KinListHttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        var result = await _service.GetByIdAsync(id, _currentUser.FamilyId, cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToActionResult(this, result), result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateKinListRequest? request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        if (request is null)
        {
            return ApiProblemDetails.InvalidRequestBody(this);
        }

        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ApiProblemDetails.BadRequest(this, "idempotency_key_required", "The Idempotency-Key header is required.");
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.Validation(this, validation.Errors);
        }

        var result = await _service.CreateAsync(request, _currentUser.FamilyId, _currentUser.UserId, idempotencyKey.Trim(), cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToCreatedActionResult(this, result), result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateKinListRequest? request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        if (request is null)
        {
            return ApiProblemDetails.InvalidRequestBody(this);
        }

        var ifMatch = ReadIfMatch();
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(this, "if_match_required", "The If-Match header is required.");
        }

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.Validation(this, validation.Errors);
        }

        var result = await _service.UpdateAsync(id, request, _currentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToActionResult(this, result), result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        var ifMatch = ReadIfMatch();
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(this, "if_match_required", "The If-Match header is required.");
        }

        var result = await _service.DeleteAsync(id, _currentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToActionResult(this, result), result);
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        var ifMatch = ReadIfMatch();
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(this, "if_match_required", "The If-Match header is required.");
        }

        var result = await _service.RestoreAsync(id, _currentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToActionResult(this, result), result);
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItemAsync(Guid id, [FromBody] CreateKinListItemRequest? request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        if (request is null)
        {
            return ApiProblemDetails.InvalidRequestBody(this);
        }

        var ifMatch = ReadIfMatch();
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(this, "if_match_required", "The If-Match header is required.");
        }

        var validation = await _createItemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.Validation(this, validation.Errors);
        }

        var result = await _service.AddItemAsync(id, request, _currentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToActionResult(this, result), result);
    }

    [HttpPost("{id:guid}/items/confirm")]
    public async Task<IActionResult> BulkConfirmItemsAsync(Guid id, [FromBody] BulkConfirmKinListItemsRequest? request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        if (request is null)
        {
            return ApiProblemDetails.InvalidRequestBody(this);
        }

        var ifMatch = ReadIfMatch();
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(this, "if_match_required", "The If-Match header is required.");
        }

        var validation = await _bulkConfirmValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.Validation(this, validation.Errors);
        }

        var result = await _service.BulkConfirmItemsAsync(id, request, _currentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToActionResult(this, result), result);
    }

    [HttpPatch("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItemAsync(Guid id, Guid itemId, [FromBody] UpdateKinListItemRequest? request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        if (request is null)
        {
            return ApiProblemDetails.InvalidRequestBody(this);
        }

        var ifMatch = ReadIfMatch();
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(this, "if_match_required", "The If-Match header is required.");
        }

        var validation = await _updateItemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.Validation(this, validation.Errors);
        }

        var result = await _service.UpdateItemAsync(id, itemId, request, _currentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToActionResult(this, result), result);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItemAsync(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        var ifMatch = ReadIfMatch();
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(this, "if_match_required", "The If-Match header is required.");
        }

        var result = await _service.DeleteItemAsync(id, itemId, _currentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToActionResult(this, result), result);
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/restore")]
    public async Task<IActionResult> RestoreItemAsync(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        var ifMatch = ReadIfMatch();
        if (ifMatch is null)
        {
            return ApiProblemDetails.BadRequest(this, "if_match_required", "The If-Match header is required.");
        }

        var result = await _service.RestoreItemAsync(id, itemId, _currentUser.FamilyId, ifMatch, cancellationToken);
        return ApplyEtag(KinListHttpResultMapper.ToActionResult(this, result), result);
    }

    [HttpPost("/api/list-drafts/from-audio")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateDraftFromAudioAsync([FromForm] KinListAudioFormRequest? request, CancellationToken cancellationToken)
    {
        var authError = EnsureAuthenticatedFamilyContext();
        if (authError is not null)
        {
            return authError;
        }

        if (request is null)
        {
            return ApiProblemDetails.InvalidRequestBody(this);
        }

        var validation = await _audioValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.Validation(this, validation.Errors);
        }

        var command = await MapAudioCommandAsync(request.Audio!, cancellationToken);
        var result = await _service.CreateDraftFromAudioAsync(command, cancellationToken);
        return KinListHttpResultMapper.ToActionResult(this, result);
    }

    [HttpPost("{id:guid}/item-drafts/from-audio")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateItemDraftsFromAudioAsync(Guid id, [FromForm] KinListAudioFormRequest? request, CancellationToken cancellationToken)
    {
        var authError = EnsureAuthenticatedFamilyContext();
        if (authError is not null)
        {
            return authError;
        }

        if (request is null)
        {
            return ApiProblemDetails.InvalidRequestBody(this);
        }

        var validation = await _audioValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.Validation(this, validation.Errors);
        }

        var command = await MapAudioCommandAsync(request.Audio!, cancellationToken);
        var result = await _service.CreateItemDraftsFromAudioAsync(id, _currentUser.FamilyId, command, cancellationToken);
        return KinListHttpResultMapper.ToActionResult(this, result);
    }

    private IActionResult? EnsureAuthenticatedFamilyContext()
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.AuthenticationRequired(this);
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "A family context is required for this resource.");
        }

        return null;
    }

    private static async Task<KinListAudioCommand> MapAudioCommandAsync(IFormFile audio, CancellationToken cancellationToken)
    {
        await using var stream = audio.OpenReadStream();
        using var memoryStream = new MemoryStream((int)audio.Length);
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return new KinListAudioCommand
        {
            AudioBytes = memoryStream.ToArray(),
            ContentType = audio.ContentType,
            FileName = audio.FileName,
        };
    }

    private string? ReadIfMatch()
    {
        var ifMatch = Request.Headers.IfMatch.ToString();
        return string.IsNullOrWhiteSpace(ifMatch) ? null : ifMatch.Trim();
    }

    private IActionResult ApplyEtag(IActionResult actionResult, Kin.KinHub.KinList.Business.Common.Result<KinListDetailResponse> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            Response.Headers.ETag = result.Value.ETag;
        }

        return actionResult;
    }
}
