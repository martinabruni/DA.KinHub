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
    private readonly IUserCredentialRepository _credentialRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateUserEmailHandler(
        IKinUserRepository userRepository,
        IUserCredentialRepository credentialRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid userId,
        UpdateUserEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var credential = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken);
            if (credential?.PasswordHash is null
                || !_passwordHasher.Verify(request.CurrentPassword, credential.PasswordHash))
            {
                return Result<bool>.Unauthorized("Invalid current password.");
            }

            var user = await _userRepository.GetAsync(userId, cancellationToken);

            var existing = await _userRepository.FindByEmailAsync(request.NewEmail, cancellationToken);
            if (existing is not null && existing.Id != userId)
                return Result<bool>.Conflict("Email already in use.");

            user.Email = request.NewEmail;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (EntityNotFoundException)
        {
            return Result<bool>.NotFound("User not found.");
        }
        catch (SharedDomainException)
        {
            return Result<bool>.UnexpectedError("Failed to update email.");
        }
    }
}
