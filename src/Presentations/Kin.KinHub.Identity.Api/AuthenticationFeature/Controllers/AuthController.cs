using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IRequestValidator<RegisterRequest> _registerValidator;
    private readonly IRequestValidator<UpdateUserEmailRequest> _updateEmailValidator;
    private readonly IRequestValidator<UpdateUserPasswordRequest> _updatePasswordValidator;
    private readonly IUserProviderService _userProviderService;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        IAuthenticationService authService,
        IRequestValidator<RegisterRequest> registerValidator,
        IRequestValidator<UpdateUserEmailRequest> updateEmailValidator,
        IRequestValidator<UpdateUserPasswordRequest> updatePasswordValidator,
        IUserProviderService userProviderService,
        ICurrentUser currentUser)
    {
        _authService = authService;
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

        var result = await _authService.RegisterAsync(request, cancellationToken);

        return HttpResultMapper.ToCreatedActionResult(this, result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> MeAsync(CancellationToken cancellationToken)
    {
        var result = await _authService.GetCurrentUserAsync(_currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
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

        var result = await _authService.UpdateUserEmailAsync(_currentUser.UserId, request, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
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

        var result = await _authService.UpdateUserPasswordAsync(_currentUser.UserId, request, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteAccountAsync(CancellationToken cancellationToken)
    {
        var result = await _authService.DeleteUserAsync(_currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("me/providers")]
    [Authorize]
    public async Task<IActionResult> GetProvidersAsync(CancellationToken cancellationToken)
    {
        var result = await _userProviderService.GetProvidersAsync(_currentUser.UserId, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
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

        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("me/providers/{provider}")]
    [Authorize]
    public async Task<IActionResult> UnlinkProviderAsync(
        IdentityProviderType provider,
        CancellationToken cancellationToken)
    {
        var result = await _userProviderService.UnlinkAsync(_currentUser.UserId, provider, cancellationToken);

        return HttpResultMapper.ToActionResult(this, result);
    }
}
