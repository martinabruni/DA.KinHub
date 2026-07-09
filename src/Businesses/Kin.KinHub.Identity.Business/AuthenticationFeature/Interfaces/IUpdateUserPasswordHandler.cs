namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IUpdateUserPasswordHandler
{
    Task<Result<bool>> HandleAsync(
        Guid userId,
        UpdateUserPasswordRequest request,
        CancellationToken cancellationToken = default);
}
