using Microsoft.Extensions.Options;

namespace DA.KinHub.Infrastructure.Storage;

public sealed class BlobStorageOptionsValidator : IValidateOptions<BlobStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, BlobStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ContainerName) || options.ContainerName.Contains('<', StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("Storage:ContainerName must contain a real blob container name.");
        }

        var hasConnectionString = !string.IsNullOrWhiteSpace(options.ConnectionString);
        var hasAccountUri = !string.IsNullOrWhiteSpace(options.AccountUri);
        if (!hasConnectionString && !hasAccountUri)
        {
            return ValidateOptionsResult.Fail("Storage configuration requires Storage:ConnectionString or Storage:AccountUri.");
        }

        if (hasConnectionString && hasAccountUri)
        {
            return ValidateOptionsResult.Fail("Storage:ConnectionString and Storage:AccountUri cannot both be configured.");
        }

        if (hasAccountUri && (!Uri.TryCreate(options.AccountUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || options.AccountUri.Contains('<', StringComparison.Ordinal)))
        {
            return ValidateOptionsResult.Fail("Storage:AccountUri must be an absolute HTTPS URI.");
        }

        return ValidateOptionsResult.Success;
    }
}
