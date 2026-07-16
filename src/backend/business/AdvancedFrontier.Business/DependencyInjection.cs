using AdvancedFrontier.Business.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace AdvancedFrontier.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(this IServiceCollection services) => services
        .AddSingleton(TimeProvider.System)
        .AddScoped<IProjectService, ProjectService>();
}
