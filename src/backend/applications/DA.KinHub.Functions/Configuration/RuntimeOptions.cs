using Microsoft.Extensions.Options;

namespace DA.KinHub.Functions.Configuration;

public sealed class RuntimeOptions
{
    public const string SectionName = "KinHub";

    public string AppName { get; init; } = "KinHub";

    public string Environment { get; init; } = "Development";

    public string ApiVersion { get; init; } = "1.0";
}

public sealed class RuntimeOptionsValidator : IValidateOptions<RuntimeOptions>
{
    public ValidateOptionsResult Validate(string? name, RuntimeOptions options)
    {
        if (!string.Equals(options.AppName, "KinHub", StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("KinHub:AppName must be KinHub.");
        }

        return string.IsNullOrWhiteSpace(options.ApiVersion)
            ? ValidateOptionsResult.Fail("KinHub:ApiVersion is required.")
            : ValidateOptionsResult.Success;
    }
}
