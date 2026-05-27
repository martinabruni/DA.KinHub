
namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public sealed class KinHubAuthenticationService : IAuthenticationService
{
    private readonly IRegisterUserHandler _registerUserHandler;
    private readonly ILoginUserHandler _loginUserHandler;
    private readonly IRefreshTokenHandler _refreshTokenHandler;
    private readonly ILogoutUserHandler _logoutUserHandler;
    private readonly IGetCurrentUserHandler _getCurrentUserHandler;
    private readonly IUpdateUserEmailHandler _updateUserEmailHandler;
    private readonly IUpdateUserPasswordHandler _updateUserPasswordHandler;
    private readonly IDeleteUserHandler _deleteUserHandler;

    public KinHubAuthenticationService(
        IRegisterUserHandler registerUserHandler,
        ILoginUserHandler loginUserHandler,
        IRefreshTokenHandler refreshTokenHandler,
        ILogoutUserHandler logoutUserHandler,
        IGetCurrentUserHandler getCurrentUserHandler,
        IUpdateUserEmailHandler updateUserEmailHandler,
        IUpdateUserPasswordHandler updateUserPasswordHandler,
        IDeleteUserHandler deleteUserHandler)
    {
        _registerUserHandler = registerUserHandler;
        _loginUserHandler = loginUserHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _logoutUserHandler = logoutUserHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _updateUserEmailHandler = updateUserEmailHandler;
        _updateUserPasswordHandler = updateUserPasswordHandler;
        _deleteUserHandler = deleteUserHandler;
    }

    /// <inheritdoc/>
    public Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default) =>
        _registerUserHandler.HandleAsync(request, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default) =>
        _loginUserHandler.HandleAsync(request, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<LoginResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default) =>
        _refreshTokenHandler.HandleAsync(refreshToken, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<bool>> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default) =>
        _logoutUserHandler.HandleAsync(refreshToken, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<UserProfileResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getCurrentUserHandler.HandleAsync(userId, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<bool>> UpdateUserEmailAsync(
        Guid userId,
        UpdateUserEmailRequest request,
        CancellationToken cancellationToken = default) =>
        _updateUserEmailHandler.HandleAsync(userId, request, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<bool>> UpdateUserPasswordAsync(
        Guid userId,
        UpdateUserPasswordRequest request,
        CancellationToken cancellationToken = default) =>
        _updateUserPasswordHandler.HandleAsync(userId, request, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<bool>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _deleteUserHandler.HandleAsync(userId, cancellationToken);
}
