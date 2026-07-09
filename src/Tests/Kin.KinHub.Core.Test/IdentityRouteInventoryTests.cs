extern alias IdentityApi;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace Kin.KinHub.Core.Test;

public sealed class IdentityRouteInventoryTests
{
    [Fact]
    public void IdentityExposesOnlyItsOwnedApiSurface()
    {
        var identityRoutes = GetRoutes(typeof(IdentityApi::Program).Assembly);

        Assert.Contains("/authorize", identityRoutes);
        Assert.Contains("/token", identityRoutes);
        Assert.Contains("/logout", identityRoutes);
        Assert.Contains("/api/access/family-context", identityRoutes);
        Assert.DoesNotContain(identityRoutes, IsRecipeOrListRoute);
    }

    private static IReadOnlySet<string> GetRoutes(Assembly assembly)
    {
        var routes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var controller in assembly.GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ControllerBase))))
        {
            var prefix = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                foreach (var attribute in method.GetCustomAttributes<HttpMethodAttribute>())
                {
                    var template = attribute.Template ?? string.Empty;
                    routes.Add(Combine(prefix, template));
                }
            }
        }

        return routes;
    }

    private static string Combine(string prefix, string template)
    {
        if (template.StartsWith('/'))
        {
            return template;
        }

        var route = string.Join('/', new[] { prefix, template }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return "/" + route.Trim('/');
    }

    private static bool IsRecipeOrListRoute(string route) =>
        route.StartsWith("/api/recipe-books", StringComparison.Ordinal)
        || route.StartsWith("/api/fridges", StringComparison.Ordinal)
        || route.StartsWith("/api/lists", StringComparison.Ordinal)
        || route.StartsWith("/api/audio-operations", StringComparison.Ordinal);
}
