using Kin.KinHub.KinRecipe.PostgreSql;
using Kin.KinHub.Core.PostgreSql.Common;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubKinRecipePostgreSqlInfrastructure(
        this IServiceCollection services,
        Action<PostgreSqlOptions> configure)
    {
        var options = new PostgreSqlOptions();
        configure(options);
        options.Validate();

        TypeAdapterConfig.GlobalSettings.NewConfig<Vector, float[]>()
            .MapWith(v => v.ToArray());
        TypeAdapterConfig.GlobalSettings.NewConfig<float[], Vector>()
            .MapWith(f => new Vector(f));

        services.AddDbContext<KinRecipeDbContext>(o =>
            o.UseNpgsql(options.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        services.AddScoped<IKinRecipeTransactionExecutor, EfCoreKinRecipeTransactionExecutor>();

        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IRecipeBookRepository, RecipeBookRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeIngredientRepository, RecipeIngredientRepository>();
        services.AddScoped<IRecipeStepRepository, RecipeStepRepository>();
        services.AddScoped<IFridgeRepository, FridgeRepository>();
        services.AddScoped<IFridgeIngredientRepository, FridgeIngredientRepository>();

        return services;
    }
}
