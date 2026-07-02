extern alias IdentityApi;
extern alias KinListApi;
extern alias KinRecipeApi;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace Kin.KinHub.Core.Test;

public sealed class HostRouteInventoryTests
{
    [Fact]
    public void HostsExposeOnlyTheirOwnedApiSurface()
    {
        var identityRoutes = GetRoutes(typeof(IdentityApi::Program).Assembly);
        var recipeRoutes = GetRoutes(typeof(KinRecipeApi::Program).Assembly);
        var listRoutes = GetRoutes(typeof(KinListApi::Program).Assembly);

        Assert.Contains("/authorize", identityRoutes);
        Assert.Contains("/token", identityRoutes);
        Assert.Contains("/logout", identityRoutes);
        Assert.Contains("/api/access/family-context", identityRoutes);
        Assert.DoesNotContain(identityRoutes, IsRecipeOrListRoute);

        Assert.Contains(recipeRoutes, route => route.StartsWith("/api/recipe-books", StringComparison.Ordinal));
        Assert.Contains(recipeRoutes, route => route.StartsWith("/api/fridges", StringComparison.Ordinal));
        Assert.DoesNotContain(recipeRoutes, IsIdentityOrListRoute);

        Assert.NotEmpty(listRoutes);
        Assert.All(listRoutes, route =>
            Assert.True(
                route.StartsWith("/api/lists", StringComparison.Ordinal)
                || route.StartsWith("/api/list-drafts", StringComparison.Ordinal),
                $"Unexpected KinList route: {route}"));
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
        || route.StartsWith("/api/lists", StringComparison.Ordinal);

    private static bool IsIdentityOrListRoute(string route) =>
        route.StartsWith("/api/auth", StringComparison.Ordinal)
        || route.StartsWith("/api/access", StringComparison.Ordinal)
        || route.StartsWith("/api/family", StringComparison.Ordinal)
        || route.StartsWith("/api/services", StringComparison.Ordinal)
        || route.StartsWith("/api/lists", StringComparison.Ordinal)
        || route is "/authorize" or "/token" or "/logout";
}
