using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kin.KinHub.Core.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShoppingListFromCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingListItemEntity",
                schema: "kinrecipe");

            migrationBuilder.DropTable(
                name: "ShoppingListEntity",
                schema: "kinrecipe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoppingListEntity",
                schema: "kinrecipe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kinrecipe_ShoppingListEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListItemEntity",
                schema: "kinrecipe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ShoppingListId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    IsChecked = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kinrecipe_ShoppingListItemEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_kinrecipe_ShoppingListItemEntity_ShoppingListId",
                        column: x => x.ShoppingListId,
                        principalSchema: "kinrecipe",
                        principalTable: "ShoppingListEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kinrecipe_ShoppingListEntity_FamilyId",
                schema: "kinrecipe",
                table: "ShoppingListEntity",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_kinrecipe_ShoppingListItemEntity_ShoppingListId",
                schema: "kinrecipe",
                table: "ShoppingListItemEntity",
                column: "ShoppingListId");
        }
    }
}
