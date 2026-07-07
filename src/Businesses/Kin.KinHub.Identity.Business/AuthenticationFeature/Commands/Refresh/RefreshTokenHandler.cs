namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IRefreshTokenHandler
{
    Task<Result<LoginResponse>> HandleAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}

public sealed class RefreshTokenHandler : IRefreshTokenHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IKinUserRepository _userRepository;
    private readonly ILoginResponseFactory _loginResponseFactory;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IKinUserRepository userRepository,
        ILoginResponseFactory loginResponseFactory)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _loginResponseFactory = loginResponseFactory;
    }

    public async Task<Result<LoginResponse>> HandleAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = await _refreshTokenRepository.FindByTokenAsync(refreshToken, cancellationToken);

            if (stored is null || stored.Revoked || stored.ExpiresAtUtc <= DateTime.UtcNow)
                return Result<LoginResponse>.Unauthorized("Invalid or expired refresh token.");

            stored.Revoked = true;
            await _refreshTokenRepository.UpdateAsync(stored.Id, stored, cancellationToken);

            var user = await _userRepository.GetAsync(stored.UserId, cancellationToken);

            if (user.Status is not UserStatus.Active)
                return Result<LoginResponse>.Unauthorized("Account is not active.");

            return Result<LoginResponse>.Success(
                await _loginResponseFactory.CreateAsync(user, cancellationToken));
        }
        catch (EntityNotFoundException)
        {
            return Result<LoginResponse>.Unauthorized("Invalid refresh token.");
        }
        catch (SharedDomainException)
        {
            return Result<LoginResponse>.UnexpectedError("Token refresh failed. Please try again.");
        }
    }
}
