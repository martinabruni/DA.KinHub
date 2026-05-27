namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IDeleteUserHandler
{
    Task<Result<bool>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class DeleteUserHandler : IDeleteUserHandler
{
    private readonly IKinUserRepository _userRepository;

    public DeleteUserHandler(IKinUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetAsync(userId);

            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user.Id, user);

            return Result<bool>.Success(true);
        }
        catch (EntityNotFoundException)
        {
            return Result<bool>.NotFound("User not found.");
        }
        catch (DomainException)
        {
            return Result<bool>.UnexpectedError("Failed to delete account.");
        }
    }
}
