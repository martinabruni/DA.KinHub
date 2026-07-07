namespace Kin.KinHub.Migrations.Runner;

public static class MigrationRunnerConfiguration
{
    public const int DefaultCommandTimeoutSeconds = 180;

    public static string ResolveConnectionString(Func<string, string?> getEnvironmentVariable)
    {
        var connectionString =
            getEnvironmentVariable("KINHUB_ConnectionStrings__KinHub")
            ?? getEnvironmentVariable("KINHUB_CONNECTIONSTRINGS__KINHUB")
            ?? getEnvironmentVariable("ConnectionStrings__KinHub");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No connection string configured. Set KINHUB_ConnectionStrings__KinHub " +
                "(or ConnectionStrings__KinHub) in the environment / Key Vault.");
        }

        return connectionString;
    }

    public static string BuildMigrationConnectionString(
        string connectionString,
        Func<string, string?> getEnvironmentVariable)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            CommandTimeout = ResolveCommandTimeoutSeconds(getEnvironmentVariable),
        };

        return builder.ConnectionString;
    }

    public static int ResolveCommandTimeoutSeconds(Func<string, string?> getEnvironmentVariable)
    {
        var rawValue =
            getEnvironmentVariable("KINHUB_MIGRATION_COMMAND_TIMEOUT_SECONDS")
            ?? getEnvironmentVariable("MigrationCommandTimeoutSeconds");

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return DefaultCommandTimeoutSeconds;
        }

        if (int.TryParse(rawValue, out var parsedValue) && parsedValue > 0)
        {
            return parsedValue;
        }

        throw new InvalidOperationException(
            "Invalid migration command timeout configured. " +
            "Set KINHUB_MIGRATION_COMMAND_TIMEOUT_SECONDS to a positive integer.");
    }
}
