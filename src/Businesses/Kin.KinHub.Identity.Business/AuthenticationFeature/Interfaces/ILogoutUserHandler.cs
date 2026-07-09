namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface ILogoutUserHandler
{
    Task<Result<bool>> HandleAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
