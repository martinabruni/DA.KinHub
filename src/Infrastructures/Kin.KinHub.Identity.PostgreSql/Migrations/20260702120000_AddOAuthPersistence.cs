using Kin.KinHub.Identity.PostgreSql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kin.KinHub.Identity.PostgreSql.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20260702120000_AddOAuthPersistence")]
public sealed class AddOAuthPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE identity."OAuthAuthorizationCode" (
                "Code" text PRIMARY KEY,
                "ClientId" text NOT NULL,
                "RedirectUri" text NOT NULL,
                "Scope" text NOT NULL,
                "CodeChallenge" text NOT NULL,
                "CodeChallengeMethod" text NOT NULL,
                "LoginResponse" jsonb NOT NULL,
                "ExpiresAtUtc" timestamptz NOT NULL
            );
            CREATE INDEX "IX_OAuthAuthorizationCode_ExpiresAtUtc"
                ON identity."OAuthAuthorizationCode" ("ExpiresAtUtc");

            CREATE TABLE identity."OAuthIdentitySession" (
                "SessionId" text PRIMARY KEY,
                "RefreshToken" text NOT NULL,
                "Email" text NOT NULL,
                "DisplayName" text NULL,
                "ExpiresAtUtc" timestamptz NOT NULL
            );
            CREATE INDEX "IX_OAuthIdentitySession_ExpiresAtUtc"
                ON identity."OAuthIdentitySession" ("ExpiresAtUtc");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS identity."OAuthIdentitySession";
            DROP TABLE IF EXISTS identity."OAuthAuthorizationCode";
            """);
    }
}
