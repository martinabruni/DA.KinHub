namespace Kin.KinHub.Migrations.Runner;

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
