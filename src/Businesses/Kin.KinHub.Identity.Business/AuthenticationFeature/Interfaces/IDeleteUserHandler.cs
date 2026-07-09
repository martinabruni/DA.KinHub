namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IDeleteUserHandler
{
    Task<Result<bool>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
