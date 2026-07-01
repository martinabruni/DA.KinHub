using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kin.KinHub.Core.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddKinListServiceCatalogEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Register Kin List (KinHubServiceType.KinList = 3) in the service catalog.
            // Idempotent: skips insert if the row already exists (e.g. seeded by the
            // create-postgres-schema.sql bootstrap script).
            migrationBuilder.Sql(
                """
                INSERT INTO core."KinHubServiceEntity"
                    ("Id", "Name", "BaseUrl", "IsActive", "CreatedAt", "UpdatedAt")
                VALUES
                    (3, 'KinList', '/kin-list', TRUE, now(), now())
                ON CONFLICT ("Id") DO NOTHING;
                """);

            // Backfill: enable Kin List for every existing family that does not yet have
            // an assignment for it. Idempotent via the NOT EXISTS guard and the
            // UQ_core_FamilyServiceEntity_FamilyService unique constraint.
            migrationBuilder.Sql(
                """
                INSERT INTO core."FamilyServiceEntity"
                    ("Id", "FamilyId", "ServiceId", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), f."Id", 3, TRUE, now(), now()
                FROM core."FamilyEntity" f
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM core."FamilyServiceEntity" fs
                    WHERE fs."FamilyId" = f."Id"
                      AND fs."ServiceId" = 3
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the family assignments first to satisfy the FK to the catalog row.
            migrationBuilder.Sql(
                """
                DELETE FROM core."FamilyServiceEntity"
                WHERE "ServiceId" = 3;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM core."KinHubServiceEntity"
                WHERE "Id" = 3;
                """);
        }
    }
}
