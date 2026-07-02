namespace Kin.KinHub.Migrations.Runner;

public sealed record MigrationStep(
    string Name,
    Func<string, int, CancellationToken, Task> ApplyAsync);

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

public sealed class MigrationRunnerService
{
    private readonly IReadOnlyList<MigrationStep> _steps;
    private readonly TextWriter _stdout;

    public MigrationRunnerService(IEnumerable<MigrationStep> steps, TextWriter? stdout = null)
    {
        _steps = steps.ToArray();
        _stdout = stdout ?? Console.Out;
    }

    public async Task RunAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var commandTimeoutSeconds = new Npgsql.NpgsqlConnectionStringBuilder(connectionString).CommandTimeout;
        await _stdout.WriteLineAsync(
            $"[migrations] Using PostgreSQL command timeout {commandTimeoutSeconds}s.");

        for (var index = 0; index < _steps.Count; index++)
        {
            var step = _steps[index];
            var startedAt = DateTimeOffset.UtcNow;
            await _stdout.WriteLineAsync($"[migrations] Applying {step.Name} migrations (step {index + 1}/{_steps.Count})...");

            try
            {
                await step.ApplyAsync(connectionString, commandTimeoutSeconds, cancellationToken);
                var elapsed = DateTimeOffset.UtcNow - startedAt;
                await _stdout.WriteLineAsync(
                    $"[migrations] {step.Name} migrations applied in {elapsed.TotalSeconds:F1}s.");
            }
            catch (Exception ex)
            {
                var elapsed = DateTimeOffset.UtcNow - startedAt;
                await _stdout.WriteLineAsync(
                    $"[migrations] {step.Name} migrations failed after {elapsed.TotalSeconds:F1}s: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        await _stdout.WriteLineAsync("[migrations] All migrations applied successfully.");
    }
}
