namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface ILoginUserHandler
{
    Task<Result<LoginResponse>> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class LoginUserHandler : ILoginUserHandler
{
    private readonly IKinUserRepository _userRepository;
    private readonly IUserCredentialRepository _credentialRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILoginResponseFactory _loginResponseFactory;

    public LoginUserHandler(
        IKinUserRepository userRepository,
        IUserCredentialRepository credentialRepository,
        IPasswordHasher passwordHasher,
        ILoginResponseFactory loginResponseFactory)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _passwordHasher = passwordHasher;
        _loginResponseFactory = loginResponseFactory;
    }

    public async Task<Result<LoginResponse>> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.FindByEmailAsync(request.Email);

            if (user is null)
                return Result<LoginResponse>.Unauthorized("Invalid email or password.");

            if (user.Status is not UserStatus.Active)
                return Result<LoginResponse>.Unauthorized("Account is not active.");

            var credential = await _credentialRepository.GetByUserIdAsync(user.Id);

            if (credential?.PasswordHash is null)
                return Result<LoginResponse>.Unauthorized("Invalid email or password.");

            if (!_passwordHasher.Verify(request.Password, credential.PasswordHash))
                return Result<LoginResponse>.Unauthorized("Invalid email or password.");

            return Result<LoginResponse>.Success(
                await _loginResponseFactory.CreateAsync(user, cancellationToken));
        }
        catch (DomainException)
        {
            return Result<LoginResponse>.UnexpectedError("Login failed. Please try again.");
        }
    }
}
