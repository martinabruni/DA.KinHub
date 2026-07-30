using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DA.KinHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKinServiceCatalog : Migration
    {
        private static readonly Guid KinListServiceId = new("6fbc9a86-31f9-4c6e-857f-0f37c7f4ec8b");
        private static readonly Guid KinListItLocalizationId = new("8bbd9b35-1a1e-4a0e-a31c-a8eaa6d4dc95");
        private static readonly Guid KinListEnLocalizationId = new("17f723db-b7df-4c88-b58c-c086f4044051");
        private static readonly DateTimeOffset SeedTimestamp = new(2026, 07, 30, 0, 0, 0, TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kin_services",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    route = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_preconfigured = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kin_services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "family_kin_service_availabilities",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kin_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_kin_service_availabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_family_kin_service_availabilities_families_family_id",
                        column: x => x.family_id,
                        principalSchema: "shared",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_family_kin_service_availabilities_kin_services_kin_service_~",
                        column: x => x.kin_service_id,
                        principalSchema: "shared",
                        principalTable: "kin_services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kin_service_localizations",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    kin_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kin_service_localizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_kin_service_localizations_kin_services_kin_service_id",
                        column: x => x.kin_service_id,
                        principalSchema: "shared",
                        principalTable: "kin_services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_family_kin_service_availabilities_family_id",
                schema: "shared",
                table: "family_kin_service_availabilities",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "IX_family_kin_service_availabilities_family_id_kin_service_id",
                schema: "shared",
                table: "family_kin_service_availabilities",
                columns: new[] { "family_id", "kin_service_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_family_kin_service_availabilities_kin_service_id",
                schema: "shared",
                table: "family_kin_service_availabilities",
                column: "kin_service_id");

            migrationBuilder.CreateIndex(
                name: "IX_kin_service_localizations_kin_service_id_language",
                schema: "shared",
                table: "kin_service_localizations",
                columns: new[] { "kin_service_id", "language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kin_services_key",
                schema: "shared",
                table: "kin_services",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kin_services_route",
                schema: "shared",
                table: "kin_services",
                column: "route",
                unique: true);

            migrationBuilder.Sql(
                $"""
                INSERT INTO shared.kin_services (\"Id\", key, route, is_active, is_preconfigured, created_at, updated_at)
                VALUES ('{KinListServiceId}', 'kinlist', '/kinlist', TRUE, TRUE, TIMESTAMPTZ '{SeedTimestamp:O}', NULL)
                ON CONFLICT (key) DO UPDATE
                SET route = EXCLUDED.route,
                    is_active = EXCLUDED.is_active,
                    is_preconfigured = EXCLUDED.is_preconfigured;
                """);

            migrationBuilder.Sql(
                $"""
                INSERT INTO shared.kin_service_localizations (\"Id\", kin_service_id, language, name, description, created_at, updated_at)
                VALUES
                    ('{KinListItLocalizationId}', '{KinListServiceId}', 'it', 'KinList', 'Lista condivisa della famiglia.', TIMESTAMPTZ '{SeedTimestamp:O}', NULL),
                    ('{KinListEnLocalizationId}', '{KinListServiceId}', 'en', 'KinList', 'Shared list for the family.', TIMESTAMPTZ '{SeedTimestamp:O}', NULL)
                ON CONFLICT (kin_service_id, language) DO UPDATE
                SET name = EXCLUDED.name,
                    description = EXCLUDED.description;
                """);

            migrationBuilder.Sql(
                $"""
                INSERT INTO shared.family_kin_service_availabilities (\"Id\", family_id, kin_service_id, is_active, created_at, updated_at)
                SELECT
                    (
                        substr(md5(f.\"Id\"::text || '{KinListServiceId}'), 1, 8) || '-' ||
                        substr(md5(f.\"Id\"::text || '{KinListServiceId}'), 9, 4) || '-' ||
                        substr(md5(f.\"Id\"::text || '{KinListServiceId}'), 13, 4) || '-' ||
                        substr(md5(f.\"Id\"::text || '{KinListServiceId}'), 17, 4) || '-' ||
                        substr(md5(f.\"Id\"::text || '{KinListServiceId}'), 21, 12)
                    )::uuid,
                    f.\"Id\",
                    '{KinListServiceId}',
                    TRUE,
                    TIMESTAMPTZ '{SeedTimestamp:O}',
                    NULL
                FROM shared.families f
                WHERE f.inactive_at IS NULL
                ON CONFLICT (family_id, kin_service_id) DO UPDATE
                SET is_active = EXCLUDED.is_active;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_kin_service_availabilities",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "kin_service_localizations",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "kin_services",
                schema: "shared");
        }
    }
}
