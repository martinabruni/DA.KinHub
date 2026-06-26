using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kin.KinHub.Identity.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class IdentityBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline no-op migration: the existing database schema is already in place.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: removing the baseline must not drop pre-existing schema objects.
        }
    }
}
