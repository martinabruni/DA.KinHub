using DA.KinHub.Business.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DA.KinHub.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(this IServiceCollection services) => services
        .AddSingleton(TimeProvider.System)
        .AddScoped<IKinHubBootstrapService, KinHubBootstrapService>()
        .AddScoped<IFamilyAccessService, FamilyAccessService>();
}
