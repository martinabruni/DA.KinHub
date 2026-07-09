namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IRefreshTokenHandler
{
    Task<Result<LoginResponse>> HandleAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
