using System.Reflection;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Functions.Configuration;

public sealed record BuildInfo(string AppName, string Version, string CommitSha, string BuildDate, string Environment, string ApiVersion);

public sealed class BuildInfoProvider(IOptions<RuntimeOptions> options)
{
    public BuildInfo Get()
    {
        var assembly = typeof(BuildInfoProvider).Assembly;
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToDictionary(item => item.Key, item => item.Value ?? "unknown");
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0+local";
        return new BuildInfo(
            options.Value.AppName,
            informational.Split('+')[0],
            Environment.GetEnvironmentVariable("COMMIT_SHA") ?? metadata.GetValueOrDefault("CommitSha", "local"),
            Environment.GetEnvironmentVariable("BUILD_DATE") ?? metadata.GetValueOrDefault("BuildDate", "local"),
            Environment.GetEnvironmentVariable("BUILD_ENVIRONMENT") ?? metadata.GetValueOrDefault("BuildEnvironment", options.Value.Environment),
            options.Value.ApiVersion);
    }
}
