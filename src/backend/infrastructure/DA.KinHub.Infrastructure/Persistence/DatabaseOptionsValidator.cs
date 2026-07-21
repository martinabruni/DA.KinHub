using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Infrastructure.Persistence;

public sealed class DatabaseOptionsValidator(IConfiguration configuration, IHostEnvironment environment) : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        if (options.CommandTimeoutSeconds is <= 0 or > 300)
        {
            return ValidateOptionsResult.Fail("Database:CommandTimeoutSeconds must be between 1 and 300.");
        }

        var connectionString = configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains('<', StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("ConnectionStrings:PostgreSql must contain a real value.");
        }

        if (!environment.IsDevelopment() && !connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail("ConnectionStrings:PostgreSql must require SSL outside Development.");
        }

        return ValidateOptionsResult.Success;
    }
}
