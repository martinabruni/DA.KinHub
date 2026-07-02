using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public sealed class PostgreSqlOAuthAuthorizationCodeStore : IOAuthAuthorizationCodeStore
{
    private readonly string _connectionString;

    public PostgreSqlOAuthAuthorizationCodeStore(string connectionString) => _connectionString = connectionString;

    public OAuthAuthorizationCodeTicket Create(
        string clientId,
        string redirectUri,
        string scope,
        string codeChallenge,
        string codeChallengeMethod,
        LoginResponse loginResponse,
        TimeSpan lifetime)
    {
        var ticket = new OAuthAuthorizationCodeTicket(
            Base64UrlEncoder.Encode(Guid.NewGuid().ToByteArray()),
            clientId,
            redirectUri,
            scope,
            codeChallenge,
            codeChallengeMethod,
            loginResponse,
            DateTimeOffset.UtcNow.Add(lifetime));

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO identity."OAuthAuthorizationCode"
                ("Code", "ClientId", "RedirectUri", "Scope", "CodeChallenge", "CodeChallengeMethod", "LoginResponse", "ExpiresAtUtc")
            VALUES (@code, @clientId, @redirectUri, @scope, @challenge, @method, @response::jsonb, @expiresAt)
            """;
        AddTicketParameters(command, ticket);
        command.ExecuteNonQuery();
        return ticket;
    }

    public bool TryConsume(string code, out OAuthAuthorizationCodeTicket? ticket)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM identity."OAuthAuthorizationCode"
            WHERE "Code" = @code
            RETURNING "ClientId", "RedirectUri", "Scope", "CodeChallenge", "CodeChallengeMethod", "LoginResponse", "ExpiresAtUtc"
            """;
        command.Parameters.AddWithValue("code", code);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            ticket = null;
            return false;
        }

        var expiresAt = reader.GetFieldValue<DateTimeOffset>(6);
        ticket = new OAuthAuthorizationCodeTicket(
            code,
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            JsonSerializer.Deserialize<LoginResponse>(reader.GetString(5))!,
            expiresAt);
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            ticket = null;
            return false;
        }

        return true;
    }

    private static void AddTicketParameters(NpgsqlCommand command, OAuthAuthorizationCodeTicket ticket)
    {
        command.Parameters.AddWithValue("code", ticket.Code);
        command.Parameters.AddWithValue("clientId", ticket.ClientId);
        command.Parameters.AddWithValue("redirectUri", ticket.RedirectUri);
        command.Parameters.AddWithValue("scope", ticket.Scope);
        command.Parameters.AddWithValue("challenge", ticket.CodeChallenge);
        command.Parameters.AddWithValue("method", ticket.CodeChallengeMethod);
        command.Parameters.AddWithValue("response", JsonSerializer.Serialize(ticket.LoginResponse));
        command.Parameters.AddWithValue("expiresAt", ticket.ExpiresAtUtc);
    }
}

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
