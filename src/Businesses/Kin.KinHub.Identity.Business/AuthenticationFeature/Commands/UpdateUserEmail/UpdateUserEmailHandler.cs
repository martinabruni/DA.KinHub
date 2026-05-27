namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IUpdateUserEmailHandler
{
    Task<Result<bool>> HandleAsync(
        Guid userId,
        UpdateUserEmailRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateUserEmailHandler : IUpdateUserEmailHandler
{
    private readonly IKinUserRepository _userRepository;

    public UpdateUserEmailHandler(IKinUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid userId,
        UpdateUserEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetAsync(userId);

            var existing = await _userRepository.FindByEmailAsync(request.NewEmail);
            if (existing is not null && existing.Id != userId)
                return Result<bool>.Conflict("Email already in use.");

            user.Email = request.NewEmail;
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
            return Result<bool>.UnexpectedError("Failed to update email.");
        }
    }
}
