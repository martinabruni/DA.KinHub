using Kin.KinHub.Core.PostgreSql.Models;
using Kin.KinHub.Migrations.Runner;
using Kin.KinHub.Identity.PostgreSql.Models;
using Kin.KinHub.KinList.PostgreSql.Models;
using Microsoft.EntityFrameworkCore;

// KinHub database migration runner.
//
// Runs ONCE (as a Container Apps Job) and exits. Migrations are applied ONLY here;
// application replicas must never call Database.Migrate().
//
// ORDERING IS LOAD-BEARING: the KinListDbContext migrations create
// kinlist."List" / kinlist."ListItem". The CoreDbContext migration
// 20260701070642_RemoveShoppingListFromCore moves data FROM kinrecipe.* INTO those
// kinlist tables, so KinList MUST be migrated first, then Core.

try
{
    var connectionString = MigrationRunnerConfiguration.ResolveConnectionString(Environment.GetEnvironmentVariable);
    var runner = new MigrationRunnerService(
    [
        new("IdentityDbContext", ApplyIdentityMigrationsAsync),
        new("KinListDbContext", ApplyKinListMigrationsAsync),
        new("CoreDbContext", ApplyCoreMigrationsAsync),
    ]);

    await runner.RunAsync(connectionString);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[migrations] FAILED: {ex}");
    return 1;
}

static async Task ApplyIdentityMigrationsAsync(string connectionString, CancellationToken cancellationToken)
{
    var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    await using var identityContext = new IdentityDbContext(identityOptions);
    await identityContext.Database.MigrateAsync(cancellationToken);
}

static async Task ApplyKinListMigrationsAsync(string connectionString, CancellationToken cancellationToken)
{
    var kinListOptions = new DbContextOptionsBuilder<KinListDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    await using var kinListContext = new KinListDbContext(kinListOptions);
    await kinListContext.Database.MigrateAsync(cancellationToken);
}

static async Task ApplyCoreMigrationsAsync(string connectionString, CancellationToken cancellationToken)
{
    var coreOptions = new DbContextOptionsBuilder<CoreDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    await using var coreContext = new CoreDbContext(coreOptions);
    await coreContext.Database.MigrateAsync(cancellationToken);
}
