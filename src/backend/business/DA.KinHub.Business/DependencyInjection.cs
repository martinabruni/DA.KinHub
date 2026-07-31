using DA.KinHub.Business.Identity;
using DA.KinHub.Business.KinList;
using Microsoft.Extensions.DependencyInjection;

namespace DA.KinHub.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(this IServiceCollection services) => services
        .AddSingleton(TimeProvider.System)
        .AddScoped<IKinHubBootstrapService, KinHubBootstrapService>()
        .AddScoped<IFamilyCreationService, FamilyCreationService>()
        .AddScoped<IFamilyAccessService, FamilyAccessService>()
        .AddScoped<IFamilySettingsService, FamilySettingsService>()
        .AddScoped<IKinHubServiceCatalogService, KinHubServiceCatalogService>()
        .AddScoped<IKinHubServiceAccessService, KinHubServiceAccessService>()
        .AddScoped<IActiveItemsPageService, ActiveItemsPageService>();
}
