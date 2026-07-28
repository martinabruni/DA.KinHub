using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DA.KinHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyNameAndCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM shared.families) THEN
                        RAISE EXCEPTION 'FEAT-002 preflight failed: shared.families already contains legacy rows.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_application_user_id",
                schema: "shared",
                table: "families",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "shared",
                table: "families",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_families_created_by_application_user_id",
                schema: "shared",
                table: "families",
                column: "created_by_application_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_families_application_users_created_by_application_user_id",
                schema: "shared",
                table: "families",
                column: "created_by_application_user_id",
                principalSchema: "shared",
                principalTable: "application_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_families_application_users_created_by_application_user_id",
                schema: "shared",
                table: "families");

            migrationBuilder.DropIndex(
                name: "IX_families_created_by_application_user_id",
                schema: "shared",
                table: "families");

            migrationBuilder.DropColumn(
                name: "created_by_application_user_id",
                schema: "shared",
                table: "families");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "shared",
                table: "families");
        }
    }
}
