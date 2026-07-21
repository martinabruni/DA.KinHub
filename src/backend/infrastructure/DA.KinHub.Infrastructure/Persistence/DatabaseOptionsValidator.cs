using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Infrastructure.Persistence;

public sealed class DatabaseOptionsValidator(IHostEnvironment environment) : IValidateOptions<DatabaseOptions>
{
    private const string ConnectionStringMode = "ConnectionString";
    private const string ManagedIdentityMode = "ManagedIdentity";

    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        if (options.CommandTimeoutSeconds is <= 0 or > 300)
        {
            return ValidateOptionsResult.Fail("Database:CommandTimeoutSeconds must be between 1 and 300.");
        }

        if (options.Port is <= 0 or > 65535)
        {
            return ValidateOptionsResult.Fail("Database:Port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(options.Mode))
        {
            return ValidateOptionsResult.Fail("Database:Mode must be explicitly configured.");
        }

        return options.Mode switch
        {
            ConnectionStringMode => ValidateConnectionString(options),
            ManagedIdentityMode => ValidateManagedIdentity(options),
            _ => ValidateOptionsResult.Fail("Database:Mode must be either ConnectionString or ManagedIdentity.")
        };
    }

    private ValidateOptionsResult ValidateConnectionString(DatabaseOptions options)
    {
        if (!environment.IsDevelopment())
        {
            return ValidateOptionsResult.Fail("Database:Mode=ConnectionString is allowed only in Development.");
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString) || options.ConnectionString.Contains('<', StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("Database:ConnectionString must contain a real value when Database:Mode=ConnectionString.");
        }

        if (HasManagedIdentityFields(options))
        {
            return ValidateOptionsResult.Fail("Database:Host, DatabaseName and Username must not be set when Database:Mode=ConnectionString.");
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateManagedIdentity(DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host) || options.Host.Contains('<', StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("Database:Host must contain a real value when Database:Mode=ManagedIdentity.");
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseName) || options.DatabaseName.Contains('<', StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("Database:DatabaseName must contain a real value when Database:Mode=ManagedIdentity.");
        }

        if (string.IsNullOrWhiteSpace(options.Username) || options.Username.Contains('<', StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("Database:Username must contain a real value when Database:Mode=ManagedIdentity.");
        }

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Database:ConnectionString must not be set when Database:Mode=ManagedIdentity.");
        }

        if (!environment.IsDevelopment() && !options.RequireSsl)
        {
            return ValidateOptionsResult.Fail("Database:RequireSsl must be true outside Development.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool HasManagedIdentityFields(DatabaseOptions options) =>
        !string.IsNullOrWhiteSpace(options.Host)
        || !string.IsNullOrWhiteSpace(options.DatabaseName)
        || !string.IsNullOrWhiteSpace(options.Username);
}
