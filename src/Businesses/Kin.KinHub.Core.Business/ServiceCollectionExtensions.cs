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

        return services;
    }

    /// <summary>
    /// Registers only family ownership, family management, and service catalog behavior.
    /// App.Functions uses this subset directly; Identity.Api never references Core at all.
    /// </summary>
    public static IServiceCollection AddKinHubFamilyBusiness(this IServiceCollection services)
    {
        services.AddLogging();
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
