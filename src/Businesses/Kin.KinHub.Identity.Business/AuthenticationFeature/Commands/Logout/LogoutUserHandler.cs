namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface ILogoutUserHandler
{
    Task<Result<bool>> HandleAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}

public sealed class LogoutUserHandler : ILogoutUserHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutUserHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<bool>> HandleAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = await _refreshTokenRepository.FindByTokenAsync(refreshToken, cancellationToken);

            if (stored is null)
                return Result<bool>.NotFound("Refresh token not found.");

            stored.Revoked = true;
            await _refreshTokenRepository.UpdateAsync(stored.Id, stored, cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (SharedDomainException)
        {
            return Result<bool>.UnexpectedError("Logout failed. Please try again.");
        }
    }
}
