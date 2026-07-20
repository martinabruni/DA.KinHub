namespace DA.KinHub.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrationsOnStartup { get; init; }

    public int CommandTimeoutSeconds { get; init; } = 30;
}
