using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubCorePostgreSqlInfrastructure(
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

        TypeAdapterConfig.GlobalSettings.NewConfig<ChatMessageEntity, ChatMessage>()
            .Map(dest => dest.Role, src => Enum.Parse<ChatMessageRole>(src.Role));
        TypeAdapterConfig.GlobalSettings.NewConfig<ChatMessage, ChatMessageEntity>()
            .Map(dest => dest.Role, src => src.Role.ToString());

        TypeAdapterConfig.GlobalSettings.NewConfig<ChatToolCallEntity, ChatToolCall>()
            .Map(dest => dest.Status, src => Enum.Parse<ChatToolCallStatus>(src.Status));
        TypeAdapterConfig.GlobalSettings.NewConfig<ChatToolCall, ChatToolCallEntity>()
            .Map(dest => dest.Status, src => src.Status.ToString());

        services.AddDbContext<CoreDbContext>(o =>
            o.UseNpgsql(options.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        // Core repositories
        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IFamilyMemberRepository, FamilyMemberRepository>();
        services.AddScoped<IKinHubServiceRepository, KinHubServiceRepository>();
        services.AddScoped<IFamilyServiceRepository, FamilyServiceRepository>();

        // Recipe repositories (stubs â€” implement after EF Core Power Tools scaffold)
        services.AddScoped<IRecipeBookRepository, RecipeBookRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeIngredientRepository, RecipeIngredientRepository>();
        services.AddScoped<IRecipeStepRepository, RecipeStepRepository>();
        services.AddScoped<IFridgeRepository, FridgeRepository>();
        services.AddScoped<IFridgeIngredientRepository, FridgeIngredientRepository>();
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IShoppingListItemRepository, ShoppingListItemRepository>();

        // Chat repositories
        services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IChatToolCallRepository, ChatToolCallRepository>();

        return services;
    }
}
