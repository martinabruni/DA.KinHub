namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IGetCurrentUserHandler
{
    Task<Result<UserProfileResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
