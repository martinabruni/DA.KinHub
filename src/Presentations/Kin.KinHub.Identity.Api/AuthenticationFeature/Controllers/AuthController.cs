using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IRegisterUserHandler _registerUserHandler;
    private readonly IGetCurrentUserHandler _getCurrentUserHandler;
    private readonly IUpdateUserEmailHandler _updateUserEmailHandler;
    private readonly IUpdateUserPasswordHandler _updateUserPasswordHandler;
    private readonly IDeleteUserHandler _deleteUserHandler;
    private readonly IRequestValidator<RegisterRequest> _registerValidator;
    private readonly IRequestValidator<UpdateUserEmailRequest> _updateEmailValidator;
    private readonly IRequestValidator<UpdateUserPasswordRequest> _updatePasswordValidator;
    private readonly IUserProviderService _userProviderService;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        IRegisterUserHandler registerUserHandler,
        IGetCurrentUserHandler getCurrentUserHandler,
        IUpdateUserEmailHandler updateUserEmailHandler,
        IUpdateUserPasswordHandler updateUserPasswordHandler,
        IDeleteUserHandler deleteUserHandler,
        IRequestValidator<RegisterRequest> registerValidator,
        IRequestValidator<UpdateUserEmailRequest> updateEmailValidator,
        IRequestValidator<UpdateUserPasswordRequest> updatePasswordValidator,
        IUserProviderService userProviderService,
        ICurrentUser currentUser)
    {
        _registerUserHandler = registerUserHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _updateUserEmailHandler = updateUserEmailHandler;
        _updateUserPasswordHandler = updateUserPasswordHandler;
        _deleteUserHandler = deleteUserHandler;
        _registerValidator = registerValidator;
        _updateEmailValidator = updateEmailValidator;
        _updatePasswordValidator = updatePasswordValidator;
        _userProviderService = userProviderService;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _registerUserHandler.HandleAsync(request, cancellationToken);

        return IdentityHttpResultMapper.ToCreatedActionResult(this, result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> MeAsync(CancellationToken cancellationToken)
    {
        var result = await _getCurrentUserHandler.HandleAsync(_currentUser.UserId, cancellationToken);

        return IdentityHttpResultMapper.ToActionResult(this, result);
    }

    [HttpPut("me/email")]
    [Authorize]
    public async Task<IActionResult> UpdateEmailAsync(
        [FromBody] UpdateUserEmailRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _updateEmailValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _updateUserEmailHandler.HandleAsync(_currentUser.UserId, request, cancellationToken);

        return IdentityHttpResultMapper.ToActionResult(this, result);
    }

    [HttpPut("me/password")]
    [Authorize]
    public async Task<IActionResult> UpdatePasswordAsync(
        [FromBody] UpdateUserPasswordRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _updatePasswordValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _updateUserPasswordHandler.HandleAsync(_currentUser.UserId, request, cancellationToken);

        return IdentityHttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteAccountAsync(CancellationToken cancellationToken)
    {
        var result = await _deleteUserHandler.HandleAsync(_currentUser.UserId, cancellationToken);

        return IdentityHttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("me/providers")]
    [Authorize]
    public async Task<IActionResult> GetProvidersAsync(CancellationToken cancellationToken)
    {
        var result = await _userProviderService.GetProvidersAsync(_currentUser.UserId, cancellationToken);

        return IdentityHttpResultMapper.ToActionResult(this, result);
    }

    [HttpPost("me/providers")]
    [Authorize]
    public async Task<IActionResult> LinkProviderAsync(
        [FromBody] LinkProviderRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var result = await _userProviderService.LinkAsync(_currentUser.UserId, request, cancellationToken);

        return IdentityHttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("me/providers/{provider}")]
    [Authorize]
    public async Task<IActionResult> UnlinkProviderAsync(
        IdentityProviderType provider,
        CancellationToken cancellationToken)
    {
        var result = await _userProviderService.UnlinkAsync(_currentUser.UserId, provider, cancellationToken);

        return IdentityHttpResultMapper.ToActionResult(this, result);
    }
}
