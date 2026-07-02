using System;
using System.Threading.Tasks;
using Kin.KinHub.Core.PostgreSql.Models;
using Kin.KinHub.KinList.PostgreSql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Dry-run / rollback test for the T06 expand/contract migration
/// (<c>20260701070642_RemoveShoppingListFromCore</c>).
///
/// Runs ONLY when a real PostgreSQL is reachable via the <c>KINHUB_TEST_POSTGRES</c>
/// environment variable (an Npgsql connection string). Otherwise every fact skips
/// cleanly so the suite stays green. When it runs it:
///   1. applies KinList migrations (creates kinlist."List"/"ListItem"),
///   2. applies Core migrations up to the migration BEFORE the reconcile,
///   3. seeds sample rows into the old kinrecipe shopping-list tables,
///   4. applies the reconcile migration,
///   5. asserts data landed in kinlist with the correct mapping/state,
///   6. asserts the compat views return the rows and a write through a view lands
///      in kinlist,
///   7. rolls the reconcile back (Down) and asserts the real tables + data return.
/// </summary>
public sealed class ExpandContractMigrationTests
{
    private const string ReconcileMigration = "20260701070642_RemoveShoppingListFromCore";

    private static string? BaseConnectionString =>
        Environment.GetEnvironmentVariable("KINHUB_TEST_POSTGRES");

    private static bool PostgresAvailable => !string.IsNullOrWhiteSpace(BaseConnectionString);

    [SkippableFact]
    public async Task ExpandContract_moves_data_exposes_views_and_rolls_back()
    {
        Skip.IfNot(
            PostgresAvailable,
            "No PostgreSQL available (set KINHUB_TEST_POSTGRES to run). " +
            "Reconcile SQL validated by review instead.");

        var databaseName = "kinhub_t06_" + Guid.NewGuid().ToString("N");
        var connectionString = await CreateDatabaseAsync(databaseName);

        try
        {
            // 1. KinList schema first (ordering matters).
            await using (var kinList = NewKinListContext(connectionString))
            {
                await kinList.Database.MigrateAsync();
            }

            var familyId = Guid.NewGuid();
            var listId = Guid.NewGuid();
            var itemAId = Guid.NewGuid();
            var itemBId = Guid.NewGuid();

            // 2. Core migrations up to the one just BEFORE the reconcile.
            var previous = await MigrateCoreToPreviousAsync(connectionString);

            // 3. Seed prior kinrecipe state.
            await SeedLegacyRowsAsync(connectionString, familyId, listId, itemAId, itemBId);

            // 4. Apply the reconcile migration (full up).
            await using (var core = NewCoreContext(connectionString))
            {
                await core.Database.MigrateAsync();
            }

            // 5. Data landed in kinlist with the correct mapping/state.
            await using (var kinList = NewKinListContext(connectionString))
            {
                var list = await kinList.Lists.SingleAsync(x => x.Id == listId);
                Assert.Equal(familyId, list.FamilyId);
                Assert.Equal("Groceries", list.Title);
                Assert.False(list.IsDeleted);
                Assert.NotEqual(Guid.Empty, list.Version);
                Assert.Equal(list.UpdatedAt, list.LastModifiedAt);

                var items = await kinList.Items
                    .Where(x => x.ListId == listId)
                    .OrderBy(x => x.ActivationOrder)
                    .ToListAsync();
                Assert.Equal(2, items.Count);
                Assert.Equal("Milk", items[0].Text);
                Assert.Equal(1, items[0].ActivationOrder);
                Assert.False(items[0].IsCompleted);
                Assert.Equal("Bread", items[1].Text);
                Assert.Equal(2, items[1].ActivationOrder);
                Assert.True(items[1].IsCompleted);
            }

            // 6a. Compat views return the migrated rows.
            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var viewName = await ScalarAsync<string>(
                    conn,
                    @"SELECT ""Name"" FROM kinrecipe.""ShoppingListEntity"" WHERE ""Id"" = @id",
                    ("id", listId));
                Assert.Equal("Groceries", viewName);

                var viewItemCount = await ScalarAsync<long>(
                    conn,
                    @"SELECT COUNT(*) FROM kinrecipe.""ShoppingListItemEntity"" WHERE ""ShoppingListId"" = @id",
                    ("id", listId));
                Assert.Equal(2L, viewItemCount);
            }

            // 6b. A write through the compat view lands in kinlist.
            var newListId = Guid.NewGuid();
            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await ExecAsync(
                    conn,
                    @"INSERT INTO kinrecipe.""ShoppingListEntity"" (""Id"", ""FamilyId"", ""Name"", ""CreatedAt"", ""UpdatedAt"")
                      VALUES (@id, @fam, 'Hardware', now(), now())",
                    ("id", newListId), ("fam", familyId));
            }
            await using (var kinList = NewKinListContext(connectionString))
            {
                var inserted = await kinList.Lists.SingleAsync(x => x.Id == newListId);
                Assert.Equal("Hardware", inserted.Title);
                Assert.False(inserted.IsDeleted);
                Assert.NotEqual(Guid.Empty, inserted.Version);
            }

            // 7. Roll back the reconcile: real tables and data return.
            await MigrateCoreToPreviousAsync(connectionString);
            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var relKind = await ScalarAsync<string>(
                    conn,
                    @"SELECT c.relkind::text FROM pg_class c
                      JOIN pg_namespace n ON n.oid = c.relnamespace
                      WHERE n.nspname = 'kinrecipe' AND c.relname = 'ShoppingListEntity'");
                Assert.Equal("r", relKind); // 'r' = ordinary table, not a view.

                var restored = await ScalarAsync<long>(
                    conn,
                    @"SELECT COUNT(*) FROM kinrecipe.""ShoppingListEntity"" WHERE ""Id"" = @id",
                    ("id", listId));
                Assert.Equal(1L, restored);
            }
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    private static CoreDbContext NewCoreContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new CoreDbContext(options);
    }

    private static KinListDbContext NewKinListContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<KinListDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new KinListDbContext(options);
    }

    /// <summary>Migrates Core to the migration immediately before the reconcile.</summary>
    private static async Task<string> MigrateCoreToPreviousAsync(string connectionString)
    {
        await using var core = NewCoreContext(connectionString);
        var migrator = core.GetService<IMigrator>();
        var migrations = core.Database.GetMigrations().ToList();
        var index = migrations.IndexOf(ReconcileMigration);
        Assert.True(index > 0, "Reconcile migration must have a predecessor to test rollback.");
        var previous = migrations[index - 1];
        await migrator.MigrateAsync(previous);
        return previous;
    }

    private static async Task SeedLegacyRowsAsync(
        string connectionString, Guid familyId, Guid listId, Guid itemAId, Guid itemBId)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await ExecAsync(
            conn,
            @"INSERT INTO kinrecipe.""ShoppingListEntity"" (""Id"", ""FamilyId"", ""Name"", ""CreatedAt"", ""UpdatedAt"")
              VALUES (@id, @fam, 'Groceries', now() - interval '2 hours', now() - interval '1 hour')",
            ("id", listId), ("fam", familyId));

        await ExecAsync(
            conn,
            @"INSERT INTO kinrecipe.""ShoppingListItemEntity"" (""Id"", ""ShoppingListId"", ""IsChecked"", ""Name"", ""CreatedAt"", ""UpdatedAt"")
              VALUES (@id, @list, false, 'Milk', now() - interval '2 hours', now())",
            ("id", itemAId), ("list", listId));

        await ExecAsync(
            conn,
            @"INSERT INTO kinrecipe.""ShoppingListItemEntity"" (""Id"", ""ShoppingListId"", ""IsChecked"", ""Name"", ""CreatedAt"", ""UpdatedAt"")
              VALUES (@id, @list, true, 'Bread', now() - interval '1 hour', now())",
            ("id", itemBId), ("list", listId));
    }

    private static async Task<string> CreateDatabaseAsync(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(BaseConnectionString);
        var adminDb = string.IsNullOrWhiteSpace(builder.Database) ? "postgres" : builder.Database;
        builder.Database = "postgres";
        await using (var admin = new NpgsqlConnection(builder.ConnectionString))
        {
            await admin.OpenAsync();
            await ExecAsync(admin, $"CREATE DATABASE \"{databaseName}\"");
        }

        builder.Database = databaseName;
        return builder.ConnectionString;
    }

    private static async Task DropDatabaseAsync(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(BaseConnectionString) { Database = "postgres" };
        await using var admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await ExecAsync(
            admin,
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{databaseName}'");
        await ExecAsync(admin, $"DROP DATABASE IF EXISTS \"{databaseName}\"");
    }

    private static async Task ExecAsync(
        NpgsqlConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(
        NpgsqlConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }
        var result = await cmd.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }
}
