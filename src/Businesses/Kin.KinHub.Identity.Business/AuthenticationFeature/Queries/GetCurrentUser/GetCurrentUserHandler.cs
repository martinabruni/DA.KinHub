namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public sealed class GetCurrentUserHandler : IGetCurrentUserHandler
{
    private readonly IKinUserRepository _userRepository;

    public GetCurrentUserHandler(IKinUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserProfileResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetAsync(userId);

            return Result<UserProfileResponse>.Success(new UserProfileResponse
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
            });
        }
        catch (EntityNotFoundException)
        {
            return Result<UserProfileResponse>.NotFound("User not found.");
        }
        catch (SharedDomainException)
        {
            return Result<UserProfileResponse>.UnexpectedError("Failed to retrieve user profile.");
        }
    }
}
