namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface ILoginResponseFactory
{
    Task<LoginResponse> CreateAsync(
        KinUser user,
        CancellationToken cancellationToken = default);
}
