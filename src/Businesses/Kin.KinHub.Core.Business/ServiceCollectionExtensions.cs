using Kin.KinHub.Core.Business.Common;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the KinHub Core business services.
    /// </summary>
    public static IServiceCollection AddKinHubCoreBusiness(
        this IServiceCollection services,
        Action<BusinessOptions>? configure = null)
    {
        var options = new BusinessOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddScoped<IFamilyService, KinHubFamilyService>();
        services.AddScoped<IKinHubServiceService, KinHubServiceService>();
        services.AddScoped<IRecipeBookService, KinHubRecipeBookService>();
        services.AddScoped<IRecipeService, KinHubRecipeService>();
        services.AddScoped<IRecipeIngredientService, KinHubRecipeIngredientService>();
        services.AddScoped<IRecipeStepService, KinHubRecipeStepService>();
        services.AddScoped<IFridgeService, KinHubFridgeService>();
        services.AddScoped<IFridgeIngredientService, KinHubFridgeIngredientService>();
        services.AddScoped<IShoppingListService, KinHubShoppingListService>();
        services.AddScoped<IShoppingListItemService, KinHubShoppingListItemService>();
        services.AddScoped<IRecipeAssistantManager, KinHubRecipeAssistantManager>();

        return services;
    }
}
