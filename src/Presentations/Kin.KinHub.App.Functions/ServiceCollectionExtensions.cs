namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubAppFunctions(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services
            .AddKinHubKinListApi(configuration, environment)
            .AddKinHubKinRecipeApi(configuration, environment)
            .AddHttpContextAccessor()
            .AddScoped<Kin.KinHub.App.Functions.Common.FunctionsAuthorizationService>()
            .AddSingleton<AudioQueueMessageProcessor>();

        return services;
    }
}
