using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdvancedFrontier.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedIdentityMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shared");

            migrationBuilder.CreateTable(
                name: "application_users",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_issuer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    external_object_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactive_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "families",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactive_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_families", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "family_memberships",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactive_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_family_memberships_application_users_application_user_id",
                        column: x => x.application_user_id,
                        principalSchema: "shared",
                        principalTable: "application_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_family_memberships_families_family_id",
                        column: x => x.family_id,
                        principalSchema: "shared",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_users_external_issuer_external_object_id",
                schema: "shared",
                table: "application_users",
                columns: new[] { "external_issuer", "external_object_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_family_memberships_application_user_id_family_id",
                schema: "shared",
                table: "family_memberships",
                columns: new[] { "application_user_id", "family_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_family_memberships_application_user_id_family_id_inactive_at",
                schema: "shared",
                table: "family_memberships",
                columns: new[] { "application_user_id", "family_id", "inactive_at" });

            migrationBuilder.CreateIndex(
                name: "IX_family_memberships_family_id",
                schema: "shared",
                table: "family_memberships",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "IX_family_memberships_single_active_user",
                schema: "shared",
                table: "family_memberships",
                column: "application_user_id",
                unique: true,
                filter: "inactive_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_memberships",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "application_users",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "families",
                schema: "shared");
        }
    }
}
