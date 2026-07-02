using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kin.KinHub.Core.PostgreSql.Migrations
{
    /// <summary>
    /// Expand/contract migration that moves shopping-list data out of Core's
    /// <c>kinrecipe</c> schema and into Kin List's <c>kinlist</c> schema, then leaves
    /// updatable backward-compatibility VIEWS behind under the old table names.
    ///
    /// DEPLOYMENT ORDERING (enforced by the migration runner job):
    ///   The KinListDbContext migrations (which create <c>kinlist."List"</c> /
    ///   <c>kinlist."ListItem"</c>) MUST be applied BEFORE this CoreDbContext migration,
    ///   because <see cref="Up"/> reads <c>kinrecipe.*</c> and writes <c>kinlist.*</c>.
    ///   The two DbContexts keep SEPARATE migration histories on the same physical DB;
    ///   only the runner enforces cross-history ordering (KinList first, then Core).
    ///
    /// EF wraps each migration in a transaction for PostgreSql, so the data move,
    /// table drops and view/trigger creation below are atomic. All data-move statements
    /// are guarded with WHERE NOT EXISTS so the migration is idempotent / re-runnable.
    /// </summary>
    /// <inheritdoc />
    public partial class RemoveShoppingListFromCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // (a) Move list data: kinrecipe.ShoppingListEntity -> kinlist."List".
            //     Name (varchar 200) -> Title (varchar 100): truncate with LEFT(...,100).
            migrationBuilder.Sql(
                """
                INSERT INTO kinlist."List"
                    ("Id", "FamilyId", "Title", "Version", "IsDeleted",
                     "CreatedAt", "UpdatedAt", "LastModifiedAt")
                SELECT
                    s."Id",
                    s."FamilyId",
                    LEFT(s."Name", 100),
                    gen_random_uuid(),
                    false,
                    s."CreatedAt",
                    s."UpdatedAt",
                    s."UpdatedAt"
                FROM kinrecipe."ShoppingListEntity" s
                WHERE NOT EXISTS (
                    SELECT 1 FROM kinlist."List" l WHERE l."Id" = s."Id"
                );
                """);

            // (a) Move item data: kinrecipe.ShoppingListItemEntity -> kinlist."ListItem".
            //     ActivationOrder = row_number() per list ordered by CreatedAt.
            migrationBuilder.Sql(
                """
                INSERT INTO kinlist."ListItem"
                    ("Id", "ListId", "Text", "Version", "IsCompleted",
                     "ActivationOrder", "IsDeleted", "CreatedAt", "UpdatedAt")
                SELECT
                    i."Id",
                    i."ShoppingListId",
                    i."Name",
                    gen_random_uuid(),
                    i."IsChecked",
                    row_number() OVER (
                        PARTITION BY i."ShoppingListId" ORDER BY i."CreatedAt"
                    ),
                    false,
                    i."CreatedAt",
                    i."UpdatedAt"
                FROM kinrecipe."ShoppingListItemEntity" i
                WHERE NOT EXISTS (
                    SELECT 1 FROM kinlist."ListItem" li WHERE li."Id" = i."Id"
                );
                """);

            // (b) Drop the real source tables (item first for the FK).
            migrationBuilder.Sql(@"DROP TABLE kinrecipe.""ShoppingListItemEntity"";");
            migrationBuilder.Sql(@"DROP TABLE kinrecipe.""ShoppingListEntity"";");

            // (c) Compatibility VIEWS under the OLD names over the new kinlist tables.
            migrationBuilder.Sql(
                """
                CREATE VIEW kinrecipe."ShoppingListEntity" AS
                SELECT
                    "Id",
                    "FamilyId",
                    "Title"      AS "Name",
                    "CreatedAt",
                    "UpdatedAt"
                FROM kinlist."List"
                WHERE "IsDeleted" = false;
                """);

            migrationBuilder.Sql(
                """
                CREATE VIEW kinrecipe."ShoppingListItemEntity" AS
                SELECT
                    "Id",
                    "ListId"      AS "ShoppingListId",
                    "IsCompleted" AS "IsChecked",
                    "Text"        AS "Name",
                    "CreatedAt",
                    "UpdatedAt"
                FROM kinlist."ListItem"
                WHERE "IsDeleted" = false;
                """);

            // (c) INSTEAD OF triggers make the views updatable, mapping writes back to the
            //     kinlist tables and filling the extra NOT NULL columns. A plain
            //     auto-updatable view cannot do this because kinlist tables carry extra
            //     required columns (Version, IsDeleted, LastModifiedAt, ActivationOrder).
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION kinrecipe.shoppinglist_compat_trigger()
                RETURNS trigger AS $$
                BEGIN
                    IF (TG_OP = 'INSERT') THEN
                        INSERT INTO kinlist."List"
                            ("Id", "FamilyId", "Title", "Version", "IsDeleted",
                             "CreatedAt", "UpdatedAt", "LastModifiedAt")
                        VALUES (
                            COALESCE(NEW."Id", gen_random_uuid()),
                            NEW."FamilyId",
                            LEFT(NEW."Name", 100),
                            gen_random_uuid(),
                            false,
                            COALESCE(NEW."CreatedAt", now()),
                            COALESCE(NEW."UpdatedAt", now()),
                            now()
                        )
                        RETURNING "Id", "FamilyId", "Title", "CreatedAt", "UpdatedAt"
                        INTO NEW."Id", NEW."FamilyId", NEW."Name", NEW."CreatedAt", NEW."UpdatedAt";
                        RETURN NEW;
                    ELSIF (TG_OP = 'UPDATE') THEN
                        UPDATE kinlist."List"
                        SET "FamilyId"       = NEW."FamilyId",
                            "Title"          = LEFT(NEW."Name", 100),
                            "Version"        = gen_random_uuid(),
                            "UpdatedAt"      = COALESCE(NEW."UpdatedAt", now()),
                            "LastModifiedAt" = now()
                        WHERE "Id" = OLD."Id";
                        RETURN NEW;
                    ELSIF (TG_OP = 'DELETE') THEN
                        -- Soft delete to preserve kinlist semantics.
                        UPDATE kinlist."List"
                        SET "IsDeleted"      = true,
                            "Version"        = gen_random_uuid(),
                            "LastModifiedAt" = now()
                        WHERE "Id" = OLD."Id";
                        RETURN OLD;
                    END IF;
                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION kinrecipe.shoppinglistitem_compat_trigger()
                RETURNS trigger AS $$
                DECLARE
                    next_order bigint;
                BEGIN
                    IF (TG_OP = 'INSERT') THEN
                        SELECT COALESCE(MAX("ActivationOrder"), 0) + 1
                        INTO next_order
                        FROM kinlist."ListItem"
                        WHERE "ListId" = NEW."ShoppingListId";

                        INSERT INTO kinlist."ListItem"
                            ("Id", "ListId", "Text", "Version", "IsCompleted",
                             "ActivationOrder", "IsDeleted", "CreatedAt", "UpdatedAt")
                        VALUES (
                            COALESCE(NEW."Id", gen_random_uuid()),
                            NEW."ShoppingListId",
                            NEW."Name",
                            gen_random_uuid(),
                            COALESCE(NEW."IsChecked", false),
                            next_order,
                            false,
                            COALESCE(NEW."CreatedAt", now()),
                            COALESCE(NEW."UpdatedAt", now())
                        )
                        RETURNING "Id", "ListId", "IsCompleted", "Text", "CreatedAt", "UpdatedAt"
                        INTO NEW."Id", NEW."ShoppingListId", NEW."IsChecked",
                             NEW."Name", NEW."CreatedAt", NEW."UpdatedAt";
                        RETURN NEW;
                    ELSIF (TG_OP = 'UPDATE') THEN
                        UPDATE kinlist."ListItem"
                        SET "ListId"      = NEW."ShoppingListId",
                            "Text"        = NEW."Name",
                            "Version"     = gen_random_uuid(),
                            "IsCompleted" = NEW."IsChecked",
                            "UpdatedAt"   = COALESCE(NEW."UpdatedAt", now())
                        WHERE "Id" = OLD."Id";
                        RETURN NEW;
                    ELSIF (TG_OP = 'DELETE') THEN
                        UPDATE kinlist."ListItem"
                        SET "IsDeleted" = true,
                            "Version"   = gen_random_uuid()
                        WHERE "Id" = OLD."Id";
                        RETURN OLD;
                    END IF;
                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER shoppinglist_compat_iud
                INSTEAD OF INSERT OR UPDATE OR DELETE ON kinrecipe."ShoppingListEntity"
                FOR EACH ROW EXECUTE FUNCTION kinrecipe.shoppinglist_compat_trigger();
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER shoppinglistitem_compat_iud
                INSTEAD OF INSERT OR UPDATE OR DELETE ON kinrecipe."ShoppingListItemEntity"
                FOR EACH ROW EXECUTE FUNCTION kinrecipe.shoppinglistitem_compat_trigger();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse (c): drop the compat views, their triggers and functions.
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS kinrecipe.""ShoppingListItemEntity"";");
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS kinrecipe.""ShoppingListEntity"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS kinrecipe.shoppinglistitem_compat_trigger();");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS kinrecipe.shoppinglist_compat_trigger();");

            // Reverse (b): recreate the real source tables (as EF originally modelled them).
            migrationBuilder.Sql(
                """
                CREATE TABLE kinrecipe."ShoppingListEntity" (
                    "Id"        uuid NOT NULL DEFAULT gen_random_uuid(),
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    "FamilyId"  uuid NOT NULL,
                    "Name"      character varying(200) NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT "PK_kinrecipe_ShoppingListEntity" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE kinrecipe."ShoppingListItemEntity" (
                    "Id"             uuid NOT NULL DEFAULT gen_random_uuid(),
                    "ShoppingListId" uuid NOT NULL,
                    "CreatedAt"      timestamp with time zone NOT NULL DEFAULT now(),
                    "IsChecked"      boolean NOT NULL,
                    "Name"           character varying(200) NOT NULL,
                    "UpdatedAt"      timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT "PK_kinrecipe_ShoppingListItemEntity" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_kinrecipe_ShoppingListItemEntity_ShoppingListId"
                        FOREIGN KEY ("ShoppingListId")
                        REFERENCES kinrecipe."ShoppingListEntity" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_kinrecipe_ShoppingListEntity_FamilyId"
                    ON kinrecipe."ShoppingListEntity" ("FamilyId");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_kinrecipe_ShoppingListItemEntity_ShoppingListId"
                    ON kinrecipe."ShoppingListItemEntity" ("ShoppingListId");
                """);

            // Reverse (a): best-effort move data back from kinlist -> kinrecipe.
            migrationBuilder.Sql(
                """
                INSERT INTO kinrecipe."ShoppingListEntity"
                    ("Id", "FamilyId", "Name", "CreatedAt", "UpdatedAt")
                SELECT l."Id", l."FamilyId", l."Title", l."CreatedAt", l."UpdatedAt"
                FROM kinlist."List" l
                WHERE l."IsDeleted" = false
                  AND NOT EXISTS (
                    SELECT 1 FROM kinrecipe."ShoppingListEntity" s WHERE s."Id" = l."Id"
                  );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO kinrecipe."ShoppingListItemEntity"
                    ("Id", "ShoppingListId", "IsChecked", "Name", "CreatedAt", "UpdatedAt")
                SELECT li."Id", li."ListId", li."IsCompleted", li."Text",
                       li."CreatedAt", li."UpdatedAt"
                FROM kinlist."ListItem" li
                WHERE li."IsDeleted" = false
                  AND EXISTS (
                    SELECT 1 FROM kinrecipe."ShoppingListEntity" s WHERE s."Id" = li."ListId"
                  )
                  AND NOT EXISTS (
                    SELECT 1 FROM kinrecipe."ShoppingListItemEntity" i WHERE i."Id" = li."Id"
                  );
                """);
        }
    }
}
