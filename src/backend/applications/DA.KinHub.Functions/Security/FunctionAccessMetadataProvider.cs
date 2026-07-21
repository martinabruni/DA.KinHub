using System.Collections.Concurrent;
using System.Reflection;
using DA.KinHub.Functions.Configuration;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Security;

public sealed class FunctionAccessMetadataProvider
{
    private static readonly Assembly ApplicationAssembly = typeof(BuildInfoProvider).Assembly;
    private readonly ConcurrentDictionary<string, FunctionAccessDescriptor> cache = new(StringComparer.Ordinal);

    public FunctionAccessDescriptor Get(FunctionDefinition definition)
    {
        return cache.GetOrAdd(definition.EntryPoint, _ => CreateDescriptor(definition.EntryPoint));
    }

    private static FunctionAccessDescriptor CreateDescriptor(string entryPoint)
    {
        var separator = entryPoint.LastIndexOf(".", StringComparison.Ordinal);
        if (separator <= 0 || separator == entryPoint.Length - 1)
        {
            throw new InvalidOperationException($"Function entry point '{entryPoint}' is invalid.");
        }

        var typeName = entryPoint[..separator];
        var methodName = entryPoint[(separator + 1)..];
        var type = ApplicationAssembly.GetType(typeName, throwOnError: true, ignoreCase: false)
            ?? throw new InvalidOperationException($"Function type '{typeName}' was not found.");
        var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Function method '{entryPoint}' was not found.");

        var isHttp = method.GetParameters().Any(parameter => parameter.GetCustomAttribute<HttpTriggerAttribute>() is not null);
        if (!isHttp)
        {
            return FunctionAccessDescriptor.NonHttp;
        }

        var allowAnonymous = method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
        var requiresFamilyAccess = method.IsDefined(typeof(RequiresFamilyAccessAttribute), inherit: true);
        if (allowAnonymous && requiresFamilyAccess)
        {
            throw new InvalidOperationException($"Function '{entryPoint}' cannot combine AllowAnonymous and RequiresFamilyAccess.");
        }

        return new FunctionAccessDescriptor(true, allowAnonymous, requiresFamilyAccess);
    }
}

public sealed record FunctionAccessDescriptor(bool IsHttp, bool AllowAnonymous, bool RequiresFamilyAccess)
{
    public static FunctionAccessDescriptor NonHttp { get; } = new(false, false, false);
}
