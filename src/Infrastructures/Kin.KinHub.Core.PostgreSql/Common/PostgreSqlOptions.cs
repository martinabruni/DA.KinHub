namespace Kin.KinHub.Core.PostgreSql.Common;

public sealed class PostgreSqlOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException($"{nameof(ConnectionString)} must be configured.");
    }
}
