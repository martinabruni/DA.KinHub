namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IIdentitySessionService
{
    Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}

public sealed class IdentitySessionService : IIdentitySessionService
{
    private readonly ILoginUserHandler _loginUserHandler;
    private readonly IRefreshTokenHandler _refreshTokenHandler;
    private readonly ILogoutUserHandler _logoutUserHandler;

    public IdentitySessionService(
        ILoginUserHandler loginUserHandler,
        IRefreshTokenHandler refreshTokenHandler,
        ILogoutUserHandler logoutUserHandler)
    {
        _loginUserHandler = loginUserHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _logoutUserHandler = logoutUserHandler;
    }

    public Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default) =>
        _loginUserHandler.HandleAsync(request, cancellationToken);

    public Task<Result<LoginResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default) =>
        _refreshTokenHandler.HandleAsync(refreshToken, cancellationToken);

    public Task<Result<bool>> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default) =>
        _logoutUserHandler.HandleAsync(refreshToken, cancellationToken);
}
