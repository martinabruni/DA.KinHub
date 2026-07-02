using System.Text;
using Kin.KinHub.Migrations.Runner;

namespace Kin.KinHub.Core.Test;

public sealed class MigrationRunnerServiceTests
{
    [Fact]
    public void ResolveConnectionString_Prefers_KinhubPrefixedEnvironmentVariable()
    {
        var values = new Dictionary<string, string?>
        {
            ["KINHUB_ConnectionStrings__KinHub"] = "Host=preferred;",
            ["KINHUB_CONNECTIONSTRINGS__KINHUB"] = "Host=legacy-uppercase;",
            ["ConnectionStrings__KinHub"] = "Host=fallback;",
        };

        var result = MigrationRunnerConfiguration.ResolveConnectionString(key => values.GetValueOrDefault(key));

        Assert.Equal("Host=preferred;", result);
    }

    [Fact]
    public void ResolveConnectionString_FallsBack_ToNonPrefixedVariable()
    {
        var values = new Dictionary<string, string?>
        {
            ["KINHUB_ConnectionStrings__KinHub"] = null,
            ["KINHUB_CONNECTIONSTRINGS__KINHUB"] = null,
            ["ConnectionStrings__KinHub"] = "Host=fallback;",
        };

        var result = MigrationRunnerConfiguration.ResolveConnectionString(key => values.GetValueOrDefault(key));

        Assert.Equal("Host=fallback;", result);
    }

    [Fact]
    public void ResolveConnectionString_Throws_WhenMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => MigrationRunnerConfiguration.ResolveConnectionString(_ => null));

        Assert.Contains("No connection string configured", exception.Message);
    }

    [Fact]
    public void BuildMigrationConnectionString_UsesDefaultCommandTimeout()
    {
        var result = MigrationRunnerConfiguration.BuildMigrationConnectionString(
            "Host=test;Database=kinhub;",
            _ => null);

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(result);

        Assert.Equal(MigrationRunnerConfiguration.DefaultCommandTimeoutSeconds, builder.CommandTimeout);
    }

    [Fact]
    public void BuildMigrationConnectionString_UsesConfiguredCommandTimeout()
    {
        var result = MigrationRunnerConfiguration.BuildMigrationConnectionString(
            "Host=test;Database=kinhub;",
            key => key == "KINHUB_MIGRATION_COMMAND_TIMEOUT_SECONDS" ? "600" : null);

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(result);

        Assert.Equal(600, builder.CommandTimeout);
    }

    [Fact]
    public void ResolveCommandTimeoutSeconds_Throws_WhenConfiguredValueIsInvalid()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => MigrationRunnerConfiguration.ResolveCommandTimeoutSeconds(
                key => key == "KINHUB_MIGRATION_COMMAND_TIMEOUT_SECONDS" ? "abc" : null));

        Assert.Contains("Invalid migration command timeout configured", exception.Message);
    }

    [Fact]
    public async Task RunAsync_AppliesSteps_InDeclaredOrder_AndLogsProgress()
    {
        var applied = new List<string>();
        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        var runner = new MigrationRunnerService(
        [
            new("IdentityDbContext", (connectionString, _) =>
            {
                applied.Add($"identity:{connectionString}");
                return Task.CompletedTask;
            }),
            new("KinListDbContext", (connectionString, _) =>
            {
                applied.Add($"kinlist:{connectionString}");
                return Task.CompletedTask;
            }),
            new("CoreDbContext", (connectionString, _) =>
            {
                applied.Add($"core:{connectionString}");
                return Task.CompletedTask;
            }),
        ], writer);

        await runner.RunAsync("Host=test;");

        Assert.Equal(
        [
            "identity:Host=test;",
            "kinlist:Host=test;",
            "core:Host=test;",
        ], applied);

        var log = output.ToString();
        Assert.Contains("Applying IdentityDbContext migrations (step 1/3)", log);
        Assert.Contains("Applying KinListDbContext migrations (step 2/3)", log);
        Assert.Contains("Applying CoreDbContext migrations (step 3/3)", log);
        Assert.Contains("IdentityDbContext migrations applied in", log);
        Assert.Contains("KinListDbContext migrations applied in", log);
        Assert.Contains("CoreDbContext migrations applied in", log);
        Assert.Contains("All migrations applied successfully.", log);
    }

    [Fact]
    public async Task RunAsync_Stops_WhenAStepFails()
    {
        var applied = new List<string>();
        using var writer = new StringWriter(new StringBuilder());

        var runner = new MigrationRunnerService(
        [
            new("IdentityDbContext", (connectionString, _) =>
            {
                applied.Add($"identity:{connectionString}");
                return Task.CompletedTask;
            }),
            new("KinListDbContext", (_, _) => throw new InvalidOperationException("boom")),
            new("CoreDbContext", (connectionString, _) =>
            {
                applied.Add($"core:{connectionString}");
                return Task.CompletedTask;
            }),
        ], writer);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync("Host=test;"));

        Assert.Equal("boom", exception.Message);
        Assert.Equal(["identity:Host=test;"], applied);
    }

    [Fact]
    public async Task RunAsync_LogsFailingStepName()
    {
        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        var runner = new MigrationRunnerService(
        [
            new("IdentityDbContext", (_, _) => Task.CompletedTask),
            new("KinListDbContext", (_, _) => throw new InvalidOperationException("boom")),
        ], writer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync("Host=test;"));

        var log = output.ToString();
        Assert.Contains("KinListDbContext migrations failed after", log);
        Assert.Contains("InvalidOperationException: boom", log);
    }
}
