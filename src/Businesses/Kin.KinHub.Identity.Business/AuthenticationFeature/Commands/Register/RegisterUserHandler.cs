namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IRegisterUserHandler
{
    Task<Result<RegisterResponse>> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class RegisterUserHandler : IRegisterUserHandler
{
    private readonly IKinUserRepository _userRepository;
    private readonly IUserCredentialRepository _credentialRepository;
    private readonly IUserProviderRepository _userProviderRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(
        IKinUserRepository userRepository,
        IUserCredentialRepository credentialRepository,
        IUserProviderRepository userProviderRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _userProviderRepository = userProviderRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<RegisterResponse>> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;

            var user = new KinUser
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                DisplayName = request.DisplayName,
                IsEmailVerified = false,
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var created = await _userRepository.CreateAsync(user);

            var credential = new UserCredential
            {
                Id = Guid.NewGuid(),
                UserId = created.Id,
                PasswordHash = _passwordHasher.Hash(request.Password),
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _credentialRepository.CreateAsync(credential);

            var userProvider = new UserProvider
            {
                Id = Guid.NewGuid(),
                UserId = created.Id,
                ProviderId = (int)IdentityProviderType.KinHub,
                ProviderUserId = created.Id.ToString(),
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _userProviderRepository.CreateAsync(userProvider);

            return Result<RegisterResponse>.Success(new RegisterResponse
            {
                UserId = created.Id,
                Email = created.Email,
            });
        }
        catch (DuplicateEntityException ex)
        {
            return Result<RegisterResponse>.Conflict(ex.Message);
        }
        catch (DomainException)
        {
            return Result<RegisterResponse>.UnexpectedError("Registration failed. Please try again.");
        }
    }
}
