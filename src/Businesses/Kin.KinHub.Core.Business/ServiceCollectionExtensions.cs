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

        services.AddScoped<ICoreTransactionExecutor, NoOpCoreTransactionExecutor>();
        services.AddKinHubFamilyBusiness();
        services.AddScoped<IRecipeBookAccessService, RecipeBookAccessService>();
        services.AddScoped<IRecipeAccessService, RecipeAccessService>();
        services.AddScoped<IRecipeResponseMapper, RecipeResponseMapper>();
        services.AddScoped<IRecipeBookResponseMapper, RecipeBookResponseMapper>();
        services.AddScoped<IRecipeIngredientResponseMapper, RecipeIngredientResponseMapper>();
        services.AddScoped<IRecipeStepResponseMapper, RecipeStepResponseMapper>();
        services.AddScoped<IRecipeIngredientAccessService, RecipeIngredientAccessService>();
        services.AddScoped<IRecipeStepAccessService, RecipeStepAccessService>();
        services.AddScoped<ICreateRecipeHandler, CreateRecipeHandler>();
        services.AddScoped<IGetRecipesHandler, GetRecipesHandler>();
        services.AddScoped<IGetRecipeByIdHandler, GetRecipeByIdHandler>();
        services.AddScoped<IUpdateRecipeHandler, UpdateRecipeHandler>();
        services.AddScoped<IDeleteRecipeHandler, DeleteRecipeHandler>();
        services.AddScoped<ICreateRecipeBookHandler, CreateRecipeBookHandler>();
        services.AddScoped<IGetRecipeBooksHandler, GetRecipeBooksHandler>();
        services.AddScoped<IGetRecipeBookByIdHandler, GetRecipeBookByIdHandler>();
        services.AddScoped<IUpdateRecipeBookHandler, UpdateRecipeBookHandler>();
        services.AddScoped<IDeleteRecipeBookHandler, DeleteRecipeBookHandler>();
        services.AddScoped<ICreateRecipeIngredientHandler, CreateRecipeIngredientHandler>();
        services.AddScoped<IGetRecipeIngredientsHandler, GetRecipeIngredientsHandler>();
        services.AddScoped<IGetRecipeIngredientByIdHandler, GetRecipeIngredientByIdHandler>();
        services.AddScoped<IUpdateRecipeIngredientHandler, UpdateRecipeIngredientHandler>();
        services.AddScoped<IDeleteRecipeIngredientHandler, DeleteRecipeIngredientHandler>();
        services.AddScoped<ICreateRecipeStepHandler, CreateRecipeStepHandler>();
        services.AddScoped<IGetRecipeStepsHandler, GetRecipeStepsHandler>();
        services.AddScoped<IGetRecipeStepByIdHandler, GetRecipeStepByIdHandler>();
        services.AddScoped<IUpdateRecipeStepHandler, UpdateRecipeStepHandler>();
        services.AddScoped<IDeleteRecipeStepHandler, DeleteRecipeStepHandler>();
        services.AddScoped<IRecipeBookService>(serviceProvider => new KinHubRecipeBookService(
            serviceProvider.GetRequiredService<ICreateRecipeBookHandler>(),
            serviceProvider.GetRequiredService<IGetRecipeBooksHandler>(),
            serviceProvider.GetRequiredService<IGetRecipeBookByIdHandler>(),
            serviceProvider.GetRequiredService<IUpdateRecipeBookHandler>(),
            serviceProvider.GetRequiredService<IDeleteRecipeBookHandler>()));
        services.AddScoped<IRecipeService>(serviceProvider => new KinHubRecipeService(
            serviceProvider.GetRequiredService<ICreateRecipeHandler>(),
            serviceProvider.GetRequiredService<IGetRecipesHandler>(),
            serviceProvider.GetRequiredService<IGetRecipeByIdHandler>(),
            serviceProvider.GetRequiredService<IUpdateRecipeHandler>(),
            serviceProvider.GetRequiredService<IDeleteRecipeHandler>()));
        services.AddScoped<IRecipeIngredientService>(serviceProvider => new KinHubRecipeIngredientService(
            serviceProvider.GetRequiredService<ICreateRecipeIngredientHandler>(),
            serviceProvider.GetRequiredService<IGetRecipeIngredientsHandler>(),
            serviceProvider.GetRequiredService<IGetRecipeIngredientByIdHandler>(),
            serviceProvider.GetRequiredService<IUpdateRecipeIngredientHandler>(),
            serviceProvider.GetRequiredService<IDeleteRecipeIngredientHandler>()));
        services.AddScoped<IRecipeStepService>(serviceProvider => new KinHubRecipeStepService(
            serviceProvider.GetRequiredService<ICreateRecipeStepHandler>(),
            serviceProvider.GetRequiredService<IGetRecipeStepsHandler>(),
            serviceProvider.GetRequiredService<IGetRecipeStepByIdHandler>(),
            serviceProvider.GetRequiredService<IUpdateRecipeStepHandler>(),
            serviceProvider.GetRequiredService<IDeleteRecipeStepHandler>()));
        services.AddScoped<IFridgeService, KinHubFridgeService>();
        services.AddScoped<IFridgeIngredientService, KinHubFridgeIngredientService>();
        services.AddScoped<IRecipeAssistantManager, KinHubRecipeAssistantManager>();

        return services;
    }

    /// <summary>
    /// Registers only family ownership, family management, and service catalog behavior.
    /// Identity.Api uses this subset so recipe and assistant graphs are never present there.
    /// </summary>
    public static IServiceCollection AddKinHubFamilyBusiness(this IServiceCollection services)
    {
        services.AddScoped<IFamilyOwnershipService, FamilyOwnershipService>();
        services.AddScoped<ICreateFamilyHandler, CreateFamilyHandler>();
        services.AddScoped<IAddFamilyMemberHandler, AddFamilyMemberHandler>();
        services.AddScoped<IGetFamilyHandler, GetFamilyHandler>();
        services.AddScoped<IDeleteFamilyMemberHandler, DeleteFamilyMemberHandler>();
        services.AddScoped<IUpdateFamilyMemberHandler, UpdateFamilyMemberHandler>();
        services.AddScoped<IUpdateFamilyHandler, UpdateFamilyHandler>();
        services.AddScoped<IDeleteFamilyHandler, DeleteFamilyHandler>();
        services.AddScoped<IFamilyService, KinHubFamilyService>();
        services.AddScoped<IKinHubServiceService, KinHubServiceService>();
        return services;
    }
}
