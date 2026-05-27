using Kin.KinHub.Core.OpenAi.Common;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubCoreOpenAiInfrastructure(
        this IServiceCollection services,
        Action<OpenAiOptions> configure)
    {
        var options = new OpenAiOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddScoped<IRecipeMissingIngredientsService, OpenAiRecipeMissingIngredientsService>();
        services.AddScoped<IRecipeAssistantService, OpenAiRecipeAssistantService>();

        return services;
    }
}
