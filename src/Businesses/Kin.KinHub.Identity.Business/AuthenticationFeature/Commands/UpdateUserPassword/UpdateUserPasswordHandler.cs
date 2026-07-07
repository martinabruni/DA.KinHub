namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IUpdateUserPasswordHandler
{
    Task<Result<bool>> HandleAsync(
        Guid userId,
        UpdateUserPasswordRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateUserPasswordHandler : IUpdateUserPasswordHandler
{
    private readonly IUserCredentialRepository _credentialRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateUserPasswordHandler(
        IUserCredentialRepository credentialRepository,
        IPasswordHasher passwordHasher)
    {
        _credentialRepository = credentialRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid userId,
        UpdateUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var credential = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken);

            if (credential?.PasswordHash is null)
                return Result<bool>.Unauthorized("Invalid current password.");

            if (!_passwordHasher.Verify(request.CurrentPassword, credential.PasswordHash))
                return Result<bool>.Unauthorized("Invalid current password.");

            credential.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            credential.UpdatedAt = DateTime.UtcNow;
            await _credentialRepository.UpdateAsync(credential.Id, credential, cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (EntityNotFoundException)
        {
            return Result<bool>.NotFound("User not found.");
        }
        catch (SharedDomainException)
        {
            return Result<bool>.UnexpectedError("Failed to update password.");
        }
    }
}
