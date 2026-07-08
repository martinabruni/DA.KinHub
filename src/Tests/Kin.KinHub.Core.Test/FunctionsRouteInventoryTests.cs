using Microsoft.Azure.Functions.Worker;
using System.Reflection;

namespace Kin.KinHub.Core.Test;

public sealed class FunctionsRouteInventoryTests
{
    [Fact]
    public void FunctionsHost_Exposes_All_NonIdentity_RouteRoots_And_QueueTrigger()
    {
        var assembly = typeof(Kin.KinHub.App.Functions.KinListFeature.ListsFunctions).Assembly;
        var routes = GetHttpRoutes(assembly);

        Assert.Contains(routes, route => route.StartsWith("api/lists", StringComparison.Ordinal));
        Assert.Contains(routes, route => route.StartsWith("api/audio-operations", StringComparison.Ordinal));
        Assert.Contains(routes, route => route.StartsWith("api/fridges", StringComparison.Ordinal));
        Assert.Contains(routes, route => route.StartsWith("api/recipe-books", StringComparison.Ordinal));
        Assert.Contains(routes, route => route.StartsWith("api/recipe-assistant", StringComparison.Ordinal));
        Assert.DoesNotContain(routes, route => route.StartsWith("api/auth", StringComparison.Ordinal));
        Assert.DoesNotContain(routes, route => route.StartsWith("api/access", StringComparison.Ordinal));

        Assert.Contains(
            assembly.GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                .SelectMany(method => method.GetParameters())
                .SelectMany(parameter => parameter.GetCustomAttributes())
                .Where(attribute => string.Equals(attribute.GetType().Name, "QueueTriggerAttribute", StringComparison.Ordinal))
                .Select(attribute => attribute.GetType().GetProperty("QueueName")?.GetValue(attribute)?.ToString()),
            queueName => string.Equals(queueName, "%AudioStorage:ProcessingQueueName%", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> GetHttpRoutes(Assembly assembly) =>
        assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(method => method.GetCustomAttribute<FunctionAttribute>() is not null)
            .SelectMany(method => method.GetParameters())
            .SelectMany(parameter => parameter.GetCustomAttributes<HttpTriggerAttribute>())
            .Select(attribute => attribute.Route ?? string.Empty)
            .Where(route => !string.IsNullOrWhiteSpace(route))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
