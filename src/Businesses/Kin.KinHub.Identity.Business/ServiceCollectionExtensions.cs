
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the KinHub business services.
    /// </summary>
    public static IServiceCollection AddKinHubIdentityBusiness(this IServiceCollection services)
    {
        services.AddScoped<IIdentityProvider, KinHubPasswordIdentityProvider>();
        services.AddScoped<IIdentityProviderRegistry, IdentityProviderRegistry>();
        services.AddScoped<IUserProviderService, UserProviderService>();
        services.AddScoped<IIdentityAccountService, IdentityAccountService>();
        services.AddScoped<IIdentitySessionService, IdentitySessionService>();
        services.AddScoped<ILoginResponseFactory, LoginResponseFactory>();
        services.AddScoped<IRegisterUserHandler, RegisterUserHandler>();
        services.AddScoped<ILoginUserHandler, LoginUserHandler>();
        services.AddScoped<IRefreshTokenHandler, RefreshTokenHandler>();
        services.AddScoped<ILogoutUserHandler, LogoutUserHandler>();
        services.AddScoped<IGetCurrentUserHandler, GetCurrentUserHandler>();
        services.AddScoped<IUpdateUserEmailHandler, UpdateUserEmailHandler>();
        services.AddScoped<IUpdateUserPasswordHandler, UpdateUserPasswordHandler>();
        services.AddScoped<IDeleteUserHandler, DeleteUserHandler>();

        return services;
    }
}
