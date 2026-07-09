namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface ILoginUserHandler
{
    Task<Result<LoginResponse>> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
