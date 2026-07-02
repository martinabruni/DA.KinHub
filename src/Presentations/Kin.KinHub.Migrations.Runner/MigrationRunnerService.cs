namespace Kin.KinHub.Migrations.Runner;

public sealed record MigrationStep(
    string Name,
    Func<string, CancellationToken, Task> ApplyAsync);

public static class MigrationRunnerConfiguration
{
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
        for (var index = 0; index < _steps.Count; index++)
        {
            var step = _steps[index];
            await _stdout.WriteLineAsync($"[migrations] Applying {step.Name} migrations (step {index + 1}/{_steps.Count})...");
            await step.ApplyAsync(connectionString, cancellationToken);
            await _stdout.WriteLineAsync($"[migrations] {step.Name} migrations applied.");
        }

        await _stdout.WriteLineAsync("[migrations] All migrations applied successfully.");
    }
}
