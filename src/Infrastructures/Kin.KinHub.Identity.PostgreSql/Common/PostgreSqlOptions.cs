namespace Kin.KinHub.Identity.PostgreSql.Common;

public sealed class PostgreSqlOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool SkipConnectionStringValidation { get; set; }

    public void Validate()
    {
        if (SkipConnectionStringValidation)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException($"{nameof(ConnectionString)} must be configured.");
    }
}
