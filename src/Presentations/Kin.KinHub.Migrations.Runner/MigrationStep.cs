namespace Kin.KinHub.Migrations.Runner;

public sealed record MigrationStep(
    string Name,
    Func<string, int, CancellationToken, Task> ApplyAsync);
