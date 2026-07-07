using Npgsql;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public sealed class PostgreSqlOAuthIdentitySessionStore : IOAuthIdentitySessionStore
{
    private readonly string _connectionString;

    public PostgreSqlOAuthIdentitySessionStore(string connectionString) => _connectionString = connectionString;

    public OAuthIdentitySession Create(LoginResponse loginResponse, TimeSpan lifetime)
    {
        var session = ToSession(Guid.NewGuid().ToString("N"), loginResponse, lifetime);
        Upsert(session);
        return session;
    }

    public bool TryGet(string sessionId, out OAuthIdentitySession? session)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "RefreshToken", "Email", "DisplayName", "ExpiresAtUtc"
            FROM identity."OAuthIdentitySession"
            WHERE "SessionId" = @sessionId AND "ExpiresAtUtc" > now()
            """;
        command.Parameters.AddWithValue("sessionId", sessionId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            session = null;
            return false;
        }

        session = new OAuthIdentitySession(
            sessionId,
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetFieldValue<DateTimeOffset>(3));
        return true;
    }

    public void Replace(string sessionId, LoginResponse loginResponse, TimeSpan lifetime) =>
        Upsert(ToSession(sessionId, loginResponse, lifetime));

    public bool TryRemove(string sessionId, out OAuthIdentitySession? session)
    {
        TryGet(sessionId, out session);
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """DELETE FROM identity."OAuthIdentitySession" WHERE "SessionId" = @sessionId""";
        command.Parameters.AddWithValue("sessionId", sessionId);
        return command.ExecuteNonQuery() > 0;
    }

    private void Upsert(OAuthIdentitySession session)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO identity."OAuthIdentitySession"
                ("SessionId", "RefreshToken", "Email", "DisplayName", "ExpiresAtUtc")
            VALUES (@sessionId, @refreshToken, @email, @displayName, @expiresAt)
            ON CONFLICT ("SessionId") DO UPDATE SET
                "RefreshToken" = EXCLUDED."RefreshToken",
                "Email" = EXCLUDED."Email",
                "DisplayName" = EXCLUDED."DisplayName",
                "ExpiresAtUtc" = EXCLUDED."ExpiresAtUtc"
            """;
        command.Parameters.AddWithValue("sessionId", session.SessionId);
        command.Parameters.AddWithValue("refreshToken", session.RefreshToken);
        command.Parameters.AddWithValue("email", session.Email);
        command.Parameters.AddWithValue("displayName", (object?)session.DisplayName ?? DBNull.Value);
        command.Parameters.AddWithValue("expiresAt", session.ExpiresAtUtc);
        command.ExecuteNonQuery();
    }

    private static OAuthIdentitySession ToSession(string id, LoginResponse response, TimeSpan lifetime) =>
        new(id, response.RefreshToken, response.Email, response.DisplayName, DateTimeOffset.UtcNow.Add(lifetime));
}
