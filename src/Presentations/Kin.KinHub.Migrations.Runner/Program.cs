using Kin.KinHub.Core.PostgreSql.Models;
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

static string ResolveConnectionString()
{
    var connectionString =
        Environment.GetEnvironmentVariable("KINHUB_ConnectionStrings__KinHub")
        ?? Environment.GetEnvironmentVariable("KINHUB_CONNECTIONSTRINGS__KINHUB")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__KinHub");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "No connection string configured. Set KINHUB_ConnectionStrings__KinHub " +
            "(or ConnectionStrings__KinHub) in the environment / Key Vault.");
    }

    return connectionString;
}

try
{
    var connectionString = ResolveConnectionString();

    Console.WriteLine("[migrations] Applying KinListDbContext migrations (step 1/2)...");
    var kinListOptions = new DbContextOptionsBuilder<KinListDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    await using (var kinListContext = new KinListDbContext(kinListOptions))
    {
        await kinListContext.Database.MigrateAsync();
    }
    Console.WriteLine("[migrations] KinListDbContext migrations applied.");

    Console.WriteLine("[migrations] Applying CoreDbContext migrations (step 2/2)...");
    var coreOptions = new DbContextOptionsBuilder<CoreDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    await using (var coreContext = new CoreDbContext(coreOptions))
    {
        await coreContext.Database.MigrateAsync();
    }
    Console.WriteLine("[migrations] CoreDbContext migrations applied.");

    Console.WriteLine("[migrations] All migrations applied successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[migrations] FAILED: {ex}");
    return 1;
}
