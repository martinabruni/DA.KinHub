using Microsoft.Extensions.Options;

namespace DA.KinHub.Functions.Configuration;

public sealed class EntraOptions
{
    public const string SectionName = "Entra";

    public bool Enabled { get; init; }

    public string Instance { get; init; } = "https://login.microsoftonline.com";

    public string TenantId { get; init; } = "common";

    public string Audience { get; init; } = "api://kinhub-local";

    public string Scope { get; init; } = "access_as_user";
}

public sealed class EntraOptionsValidator : IValidateOptions<EntraOptions>
{
    public ValidateOptionsResult Validate(string? name, EntraOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var values = new[] { options.Instance, options.TenantId, options.Audience, options.Scope };
        if (values.Any(value => string.IsNullOrWhiteSpace(value) || value.Contains('<', StringComparison.Ordinal)))
        {
            return ValidateOptionsResult.Fail("Entra configuration must contain real environment values when authentication is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}
