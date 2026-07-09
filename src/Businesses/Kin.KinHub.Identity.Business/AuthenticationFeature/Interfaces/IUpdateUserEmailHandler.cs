namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IUpdateUserEmailHandler
{
    Task<Result<bool>> HandleAsync(
        Guid userId,
        UpdateUserEmailRequest request,
        CancellationToken cancellationToken = default);
}
