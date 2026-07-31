using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DA.KinHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "family_invitations",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_application_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    code_hmac = table.Column<byte[]>(type: "bytea", nullable: false),
                    hmac_key_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_invitations", x => x.Id);
                    table.CheckConstraint("CK_family_invitations_consumed_after_created", "consumed_at IS NULL OR consumed_at >= created_at");
                    table.CheckConstraint("CK_family_invitations_expires_after_created", "expires_at > created_at");
                    table.CheckConstraint("CK_family_invitations_hmac_key_version_non_empty", "char_length(hmac_key_version) > 0");
                    table.CheckConstraint("CK_family_invitations_hmac_non_empty", "octet_length(code_hmac) > 0");
                    table.CheckConstraint("CK_family_invitations_revoked_after_created", "revoked_at IS NULL OR revoked_at >= created_at");
                    table.ForeignKey(
                        name: "FK_family_invitations_application_users_created_by_application~",
                        column: x => x.created_by_application_user_id,
                        principalSchema: "shared",
                        principalTable: "application_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_family_invitations_families_family_id",
                        column: x => x.family_id,
                        principalSchema: "shared",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_family_invitations_active_by_family_created_at_id",
                schema: "shared",
                table: "family_invitations",
                columns: new[] { "family_id", "created_at", "Id" },
                filter: "revoked_at IS NULL AND consumed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_family_invitations_created_by_application_user_id",
                schema: "shared",
                table: "family_invitations",
                column: "created_by_application_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_invitations",
                schema: "shared");
        }
    }
}
