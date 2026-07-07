using Kin.KinHub.KinRecipe.AzureOpenAi.Common;
using Kin.KinHub.KinRecipe.AzureOpenAi.RecipeAssistantFeature;
using Mapster;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubKinRecipeAzureOpenAiInfrastructure(
        this IServiceCollection services,
        Action<OpenAiOptions> configure)
    {
        TypeAdapterConfig.GlobalSettings.Apply(new OpenAiMappingProfile());

        var options = new OpenAiOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddScoped<IRecipeMissingIngredientsService, OpenAiRecipeMissingIngredientsService>();
        services.AddScoped<IRecipeAssistantService, OpenAiRecipeAssistantService>();

        return services;
    }
}
