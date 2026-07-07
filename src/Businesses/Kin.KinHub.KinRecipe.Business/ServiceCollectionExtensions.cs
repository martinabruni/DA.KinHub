using Mapster;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubKinRecipeBusiness(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Apply(new KinRecipeMappingProfile());

        services.AddScoped<IKinRecipeTransactionExecutor, NoOpKinRecipeTransactionExecutor>();
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
}
