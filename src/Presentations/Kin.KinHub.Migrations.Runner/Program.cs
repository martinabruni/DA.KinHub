using Kin.KinHub.Core.PostgreSql;
using Kin.KinHub.Identity.PostgreSql;
using Kin.KinHub.KinList.PostgreSql;
using Kin.KinHub.KinRecipe.PostgreSql;
using Kin.KinHub.Migrations.Runner;
using Microsoft.EntityFrameworkCore;

try
{
    var connectionString = MigrationRunnerConfiguration.BuildMigrationConnectionString(
        MigrationRunnerConfiguration.ResolveConnectionString(Environment.GetEnvironmentVariable),
        Environment.GetEnvironmentVariable);
    var runner = new MigrationRunnerService(
    [
        new("IdentityDbContext", ApplyIdentityMigrationsAsync),
        new("KinListDbContext", ApplyKinListMigrationsAsync),
        new("CoreDbContext", ApplyCoreMigrationsAsync),
        new("KinRecipeDbContext", ApplyKinRecipeMigrationsAsync),
    ]);

    await runner.RunAsync(connectionString);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[migrations] FAILED: {ex}");
    return 1;
}

static async Task ApplyIdentityMigrationsAsync(
    string connectionString,
    int commandTimeoutSeconds,
    CancellationToken cancellationToken)
{
    var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
        .UseNpgsql(connectionString, options => options.CommandTimeout(commandTimeoutSeconds))
        .Options;
    await using var identityContext = new IdentityDbContext(identityOptions);
    identityContext.Database.SetCommandTimeout(commandTimeoutSeconds);
    await identityContext.Database.MigrateAsync(cancellationToken);
}

static async Task ApplyKinListMigrationsAsync(
    string connectionString,
    int commandTimeoutSeconds,
    CancellationToken cancellationToken)
{
    var kinListOptions = new DbContextOptionsBuilder<KinListDbContext>()
        .UseNpgsql(connectionString, options => options.CommandTimeout(commandTimeoutSeconds))
        .Options;
    await using var kinListContext = new KinListDbContext(kinListOptions);
    kinListContext.Database.SetCommandTimeout(commandTimeoutSeconds);
    await kinListContext.Database.MigrateAsync(cancellationToken);
}

static async Task ApplyCoreMigrationsAsync(
    string connectionString,
    int commandTimeoutSeconds,
    CancellationToken cancellationToken)
{
    var coreOptions = new DbContextOptionsBuilder<CoreDbContext>()
        .UseNpgsql(connectionString, options => options.CommandTimeout(commandTimeoutSeconds))
        .Options;
    await using var coreContext = new CoreDbContext(coreOptions);
    coreContext.Database.SetCommandTimeout(commandTimeoutSeconds);
    await coreContext.Database.MigrateAsync(cancellationToken);
}

static async Task ApplyKinRecipeMigrationsAsync(
    string connectionString,
    int commandTimeoutSeconds,
    CancellationToken cancellationToken)
{
    var kinRecipeOptions = new DbContextOptionsBuilder<KinRecipeDbContext>()
        .UseNpgsql(connectionString, options => options.CommandTimeout(commandTimeoutSeconds))
        .Options;
    await using var kinRecipeContext = new KinRecipeDbContext(kinRecipeOptions);
    kinRecipeContext.Database.SetCommandTimeout(commandTimeoutSeconds);
    await kinRecipeContext.Database.MigrateAsync(cancellationToken);
}
