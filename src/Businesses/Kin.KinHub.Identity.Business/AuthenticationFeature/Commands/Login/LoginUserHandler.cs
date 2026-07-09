namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public sealed class LoginUserHandler : ILoginUserHandler
{
    private readonly IIdentityProviderRegistry _providerRegistry;
    private readonly ILoginResponseFactory _loginResponseFactory;

    public LoginUserHandler(
        IIdentityProviderRegistry providerRegistry,
        ILoginResponseFactory loginResponseFactory)
    {
        _providerRegistry = providerRegistry;
        _loginResponseFactory = loginResponseFactory;
    }

    public async Task<Result<LoginResponse>> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerRegistry.Resolve(IdentityProviderType.KinHub);
        if (provider is null)
            return Result<LoginResponse>.UnexpectedError("The KinHub identity provider is not available.");

        try
        {
            var user = await provider.AuthenticateAsync(
                new IdentityCredential
                {
                    Email = request.Email,
                    Password = request.Password,
                },
                cancellationToken);

            if (user is null)
                return Result<LoginResponse>.Unauthorized("Invalid email or password.");

            return Result<LoginResponse>.Success(
                await _loginResponseFactory.CreateAsync(user, cancellationToken));
        }
        catch (SharedDomainException)
        {
            return Result<LoginResponse>.UnexpectedError("Login failed. Please try again.");
        }
    }
}
