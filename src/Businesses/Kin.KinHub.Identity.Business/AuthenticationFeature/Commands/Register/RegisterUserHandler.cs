namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IRegisterUserHandler
{
    Task<Result<RegisterResponse>> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class RegisterUserHandler : IRegisterUserHandler
{
    private readonly IIdentityProviderRegistry _providerRegistry;

    public RegisterUserHandler(IIdentityProviderRegistry providerRegistry)
    {
        _providerRegistry = providerRegistry;
    }

    public async Task<Result<RegisterResponse>> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerRegistry.Resolve(IdentityProviderType.KinHub);
        if (provider is null)
            return Result<RegisterResponse>.UnexpectedError("The KinHub identity provider is not available.");

        try
        {
            var created = await provider.RegisterAsync(
                new IdentityRegistration
                {
                    Email = request.Email,
                    DisplayName = request.DisplayName,
                    Password = request.Password,
                },
                cancellationToken);

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
