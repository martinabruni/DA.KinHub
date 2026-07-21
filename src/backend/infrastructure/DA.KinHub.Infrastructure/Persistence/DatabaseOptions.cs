namespace DA.KinHub.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Mode { get; init; } = string.Empty;

    public string? ConnectionString { get; init; }

    public string? Host { get; init; }

    public int Port { get; init; } = 5432;

    public string? DatabaseName { get; init; }

    public string? Username { get; init; }

    public bool RequireSsl { get; init; } = true;

    public bool ApplyMigrationsOnStartup { get; init; }

    public int CommandTimeoutSeconds { get; init; } = 30;
}
