namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IRegisterUserHandler
{
    Task<Result<RegisterResponse>> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);
}
