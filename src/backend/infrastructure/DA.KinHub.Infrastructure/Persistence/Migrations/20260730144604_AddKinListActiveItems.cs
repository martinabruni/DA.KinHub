using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DA.KinHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKinListActiveItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kinlist");

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "kinlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false),
                    created_by_application_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactive_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.UniqueConstraint("AK_categories_Id_family_id", x => new { x.Id, x.family_id });
                    table.ForeignKey(
                        name: "FK_categories_application_users_created_by_application_user_id",
                        column: x => x.created_by_application_user_id,
                        principalSchema: "shared",
                        principalTable: "application_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_categories_families_family_id",
                        column: x => x.family_id,
                        principalSchema: "shared",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_groups",
                schema: "kinlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recording_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_application_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactive_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_groups", x => x.Id);
                    table.UniqueConstraint("AK_registration_groups_Id_family_id", x => new { x.Id, x.family_id });
                    table.ForeignKey(
                        name: "FK_registration_groups_application_users_created_by_applicatio~",
                        column: x => x.created_by_application_user_id,
                        principalSchema: "shared",
                        principalTable: "application_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_registration_groups_families_family_id",
                        column: x => x.family_id,
                        principalSchema: "shared",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "kinlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    position_in_group = table.Column<int>(type: "integer", nullable: false),
                    owner_application_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visibility = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by_application_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by_application_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    inactive_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.Id);
                    table.UniqueConstraint("AK_items_Id_family_id", x => new { x.Id, x.family_id });
                    table.CheckConstraint("CK_items_position_in_group_non_negative", "position_in_group >= 0");
                    table.CheckConstraint("CK_items_revision_positive", "revision >= 1");
                    table.CheckConstraint("CK_items_status", "status IN ('Active', 'Completed')");
                    table.CheckConstraint("CK_items_visibility", "visibility IN ('Shared', 'Personal')");
                    table.ForeignKey(
                        name: "FK_items_application_users_completed_by_application_user_id",
                        column: x => x.completed_by_application_user_id,
                        principalSchema: "shared",
                        principalTable: "application_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_application_users_modified_by_application_user_id",
                        column: x => x.modified_by_application_user_id,
                        principalSchema: "shared",
                        principalTable: "application_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_application_users_owner_application_user_id",
                        column: x => x.owner_application_user_id,
                        principalSchema: "shared",
                        principalTable: "application_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_registration_groups_registration_group_id_family_id",
                        columns: x => new { x.registration_group_id, x.family_id },
                        principalSchema: "kinlist",
                        principalTable: "registration_groups",
                        principalColumns: new[] { "Id", "family_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_categories",
                schema: "kinlist",
                columns: table => new
                {
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_categories", x => new { x.item_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_item_categories_categories_category_id_family_id",
                        columns: x => new { x.category_id, x.family_id },
                        principalSchema: "kinlist",
                        principalTable: "categories",
                        principalColumns: new[] { "Id", "family_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_categories_items_item_id_family_id",
                        columns: x => new { x.item_id, x.family_id },
                        principalSchema: "kinlist",
                        principalTable: "items",
                        principalColumns: new[] { "Id", "family_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_created_by_application_user_id",
                schema: "kinlist",
                table: "categories",
                column: "created_by_application_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_family_id_normalized_name",
                schema: "kinlist",
                table: "categories",
                columns: new[] { "family_id", "normalized_name" },
                unique: true,
                filter: "inactive_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_category_id_family_id",
                schema: "kinlist",
                table: "item_categories",
                columns: new[] { "category_id", "family_id" });

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_family_id_category_id_item_id",
                schema: "kinlist",
                table: "item_categories",
                columns: new[] { "family_id", "category_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_family_id_item_id",
                schema: "kinlist",
                table: "item_categories",
                columns: new[] { "family_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_item_id_family_id",
                schema: "kinlist",
                table: "item_categories",
                columns: new[] { "item_id", "family_id" });

            migrationBuilder.CreateIndex(
                name: "IX_items_completed_by_application_user_id",
                schema: "kinlist",
                table: "items",
                column: "completed_by_application_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_modified_by_application_user_id",
                schema: "kinlist",
                table: "items",
                column: "modified_by_application_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_owner_application_user_id",
                schema: "kinlist",
                table: "items",
                column: "owner_application_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_personal_active",
                schema: "kinlist",
                table: "items",
                columns: new[] { "registration_group_id", "owner_application_user_id", "position_in_group", "Id" },
                filter: "inactive_at IS NULL AND status = 'Active' AND visibility = 'Personal'");

            migrationBuilder.CreateIndex(
                name: "IX_items_registration_group_id_family_id",
                schema: "kinlist",
                table: "items",
                columns: new[] { "registration_group_id", "family_id" });

            migrationBuilder.CreateIndex(
                name: "IX_items_registration_group_id_position_in_group",
                schema: "kinlist",
                table: "items",
                columns: new[] { "registration_group_id", "position_in_group" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_shared_active",
                schema: "kinlist",
                table: "items",
                columns: new[] { "registration_group_id", "position_in_group", "Id" },
                filter: "inactive_at IS NULL AND status = 'Active' AND visibility = 'Shared'");

            migrationBuilder.CreateIndex(
                name: "IX_registration_groups_created_by_application_user_id",
                schema: "kinlist",
                table: "registration_groups",
                column: "created_by_application_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_groups_family_id_created_at_Id",
                schema: "kinlist",
                table: "registration_groups",
                columns: new[] { "family_id", "created_at", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_registration_groups_family_id_recording_id",
                schema: "kinlist",
                table: "registration_groups",
                columns: new[] { "family_id", "recording_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_categories",
                schema: "kinlist");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "kinlist");

            migrationBuilder.DropTable(
                name: "items",
                schema: "kinlist");

            migrationBuilder.DropTable(
                name: "registration_groups",
                schema: "kinlist");
        }
    }
}
