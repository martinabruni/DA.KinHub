using DA.KinHub.Business.Identity;
using DA.KinHub.Business.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace DA.KinHub.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(this IServiceCollection services) => services
        .AddSingleton(TimeProvider.System)
        .AddScoped<IKinListBootstrapService, KinListBootstrapService>()
        .AddScoped<IFamilyAccessService, FamilyAccessService>()
        .AddScoped<IProjectService, ProjectService>();
}
