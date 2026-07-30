using System.Linq;
using Azure.Core;
using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;
using DA.KinHub.Infrastructure;
using DA.KinHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace DA.KinHub.IntegrationTests;

public sealed class FamilyRepositoryPostgreSqlTests
{
    [SkippableFact]
    public async Task MigrateAppliesFamilyColumnsForeignKeyAndSingleActiveMembershipConstraint()
    {
        await using var harness = await PostgreSqlIntegrationTestHarness.CreateAsync();

        await harness.MigrateAsync();

        Assert.Equal(1L, await harness.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = 'shared'
              AND table_name = 'families'
              AND column_name = 'name';
            """));

        Assert.Equal(1L, await harness.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = 'shared'
              AND table_name = 'families'
              AND column_name = 'created_by_application_user_id';
            """));

        Assert.Equal(1L, await harness.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM information_schema.table_constraints
            WHERE constraint_schema = 'shared'
              AND table_name = 'families'
              AND constraint_name = 'FK_families_application_users_created_by_application_user_id';
            """));

        Assert.Equal(1L, await harness.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM pg_indexes
            WHERE schemaname = 'shared'
              AND tablename = 'family_memberships'
              AND indexname = 'IX_family_memberships_single_active_user';
            """));
    }

    [SkippableFact]
    public async Task MigrateFailsWhenLegacyFamilyRowsExist()
    {
        await using var harness = await PostgreSqlIntegrationTestHarness.CreateAsync();

        await harness.MigrateAsync("20260720200215_AddSharedIdentityMembership");
        await harness.ExecuteAsync(
            "INSERT INTO shared.families (\"Id\", created_at) VALUES (@id, @createdAt);",
            new NpgsqlParameter("id", Guid.NewGuid()),
            new NpgsqlParameter("createdAt", DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAnyAsync<PostgresException>(() => harness.MigrateAsync());

        Assert.Contains("FEAT-002 preflight failed", exception.Message, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task CreateWithCreatorAsyncCreatesFamilyAndRetryReturnsExistingFamily()
    {
        await using var harness = await PostgreSqlIntegrationTestHarness.CreateAsync();

        await harness.MigrateAsync();
        var user = await harness.SeedUserAsync();

        var created = await harness.CreateFamilyAsync(user.Id, "Famiglia Bruni");
        var retried = await harness.CreateFamilyAsync(user.Id, "Nuovo Nome Ignorato");

        var persistedName = await harness.ExecuteScalarAsync<string>("SELECT name FROM shared.families LIMIT 1;");
        var orphanCount = await harness.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM shared.families f
            LEFT JOIN shared.family_memberships fm ON fm.family_id = f."Id" AND fm.inactive_at IS NULL
            WHERE fm."Id" IS NULL;
            """);

        var createdResult = Assert.IsType<FamilyCreationPersistenceResult.Created>(created);
        var existingResult = Assert.IsType<FamilyCreationPersistenceResult.Existing>(retried);
        Assert.Equal(createdResult.FamilyId, existingResult.FamilyId);
        Assert.False(existingResult.ReconciledConflict);
        Assert.Equal("Famiglia Bruni", persistedName);
        Assert.Equal(1L, await harness.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM shared.families;"));
        Assert.Equal(1L, await harness.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM shared.family_memberships;"));
        Assert.Equal(0L, orphanCount);
    }

    [SkippableFact]
    public async Task CreateWithCreatorAsyncConcurrentRequestsCreateSingleFamilyWithoutOrphans()
    {
        await using var harness = await PostgreSqlIntegrationTestHarness.CreateAsync();

        await harness.MigrateAsync();
        var user = await harness.SeedUserAsync();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstAttempt = Task.Run(async () =>
        {
            await gate.Task;
            return await harness.CreateFamilyAsync(user.Id, "Famiglia Bruni Uno");
        });

        var secondAttempt = Task.Run(async () =>
        {
            await gate.Task;
            return await harness.CreateFamilyAsync(user.Id, "Famiglia Bruni Due");
        });

        gate.SetResult();
        var results = await Task.WhenAll(firstAttempt, secondAttempt);

        Assert.Single(results, result => result is FamilyCreationPersistenceResult.Created);
        Assert.Single(results, result => result is FamilyCreationPersistenceResult.Existing);
        Assert.Single(results.Select(result => result.FamilyId).Distinct());
        Assert.Equal(1L, await harness.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM shared.families;"));
        Assert.Equal(1L, await harness.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM shared.family_memberships;"));
        Assert.Equal(0L, await harness.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM shared.families f
            LEFT JOIN shared.family_memberships fm ON fm.family_id = f."Id" AND fm.inactive_at IS NULL
            WHERE fm."Id" IS NULL;
            """));
    }

    private sealed class PostgreSqlIntegrationTestHarness : IAsyncDisposable
    {
        private readonly PostgreSqlContainer? container;
        private readonly string administrativeConnectionString;
        private readonly ServiceProvider serviceProvider;
        private readonly string databaseName;

        private PostgreSqlIntegrationTestHarness(
            PostgreSqlContainer? container,
            string administrativeConnectionString,
            string connectionString,
            string databaseName,
            ServiceProvider serviceProvider)
        {
            this.container = container;
            this.administrativeConnectionString = administrativeConnectionString;
            ConnectionString = connectionString;
            this.databaseName = databaseName;
            this.serviceProvider = serviceProvider;
        }

        public string ConnectionString { get; }

        public static async Task<PostgreSqlIntegrationTestHarness> CreateAsync()
        {
            var explicitConnectionString = Environment.GetEnvironmentVariable("KINHUB_TEST_POSTGRES_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(explicitConnectionString))
            {
                var provisionedDatabase = await ProvisionDatabaseAsync(explicitConnectionString);
                var provider = CreateServiceProvider(provisionedDatabase.ConnectionString);
                return new PostgreSqlIntegrationTestHarness(
                    null,
                    provisionedDatabase.AdministrativeConnectionString,
                    provisionedDatabase.ConnectionString,
                    provisionedDatabase.DatabaseName,
                    provider);
            }

            Skip.IfNot(IsDockerAvailable(), "Docker non disponibile e KINHUB_TEST_POSTGRES_CONNECTION_STRING non configurata.");

            var container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("postgres")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await container.StartAsync();
            var containerDatabase = await ProvisionDatabaseAsync(container.GetConnectionString());
            var serviceProvider = CreateServiceProvider(containerDatabase.ConnectionString);
            return new PostgreSqlIntegrationTestHarness(
                container,
                containerDatabase.AdministrativeConnectionString,
                containerDatabase.ConnectionString,
                containerDatabase.DatabaseName,
                serviceProvider);
        }

        public async Task MigrateAsync(string? targetMigration = null)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KinHubDbContext>();

            if (string.IsNullOrWhiteSpace(targetMigration))
            {
                await dbContext.Database.MigrateAsync();
                return;
            }

            var migrator = dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();
            await migrator.MigrateAsync(targetMigration);
        }

        public async Task<ApplicationUser> SeedUserAsync()
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KinHubDbContext>();
            var user = ApplicationUser.Create(new ExternalIdentity("https://issuer", Guid.NewGuid()), DateTimeOffset.UtcNow);
            dbContext.ApplicationUsers.Add(user);
            await dbContext.SaveChangesAsync();
            return user;
        }

        public async Task<FamilyCreationPersistenceResult> CreateFamilyAsync(Guid applicationUserId, string familyName)
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IFamilyRepository>();
            var now = DateTimeOffset.UtcNow;
            var family = Family.Create(FamilyName.Create(familyName), applicationUserId, now);
            var membership = FamilyMembership.Create(applicationUserId, family.Id, now);
            return await repository.CreateWithCreatorAsync(applicationUserId, family, membership, CancellationToken.None);
        }

        public async Task ExecuteAsync(string sql, params NpgsqlParameter[] parameters)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            var result = await command.ExecuteScalarAsync();
            return (T)Convert.ChangeType(result ?? throw new InvalidOperationException("Expected a scalar result."), typeof(T));
        }

        public async ValueTask DisposeAsync()
        {
            await serviceProvider.DisposeAsync();

            try
            {
                await DropDatabaseAsync(administrativeConnectionString, databaseName);
            }
            finally
            {
                if (container is not null)
                {
                    await container.DisposeAsync();
                }
            }
        }

        private static ServiceProvider CreateServiceProvider(string connectionString)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Mode"] = "ConnectionString",
                ["Database:ConnectionString"] = connectionString,
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Storage:AccountUri"] = "https://kinhubtest.blob.core.windows.net/",
                ["Storage:ContainerName"] = "documents"
            }).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddSingleton<IHostEnvironment>(new HostingEnvironmentStub(isDevelopment: true));
            services.AddSingleton<TokenCredential>(new StaticTokenCredential());
            services.AddInfrastructure(configuration);
            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }

        private static bool IsDockerAvailable()
            => File.Exists("\\\\.\\pipe\\dockerDesktopLinuxEngine") || File.Exists("\\\\.\\pipe\\docker_engine");

        private static async Task<(string AdministrativeConnectionString, string ConnectionString, string DatabaseName)> ProvisionDatabaseAsync(string baseConnectionString)
        {
            var databaseName = $"kinhub_feat002_{Guid.NewGuid():N}";
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Pooling = false
            };

            var administrativeBuilder = new NpgsqlConnectionStringBuilder(builder.ConnectionString)
            {
                Database = "postgres",
                Pooling = false
            };

            await using (var connection = new NpgsqlConnection(administrativeBuilder.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", connection);
                await command.ExecuteNonQueryAsync();
            }

            builder.Database = databaseName;
            return (administrativeBuilder.ConnectionString, builder.ConnectionString, databaseName);
        }

        private static async Task DropDatabaseAsync(string administrativeConnectionString, string databaseName)
        {
            await using var connection = new NpgsqlConnection(administrativeConnectionString);
            await connection.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{databaseName}'
                  AND pid <> pg_backend_pid();
                """,
                connection))
            {
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\";", connection);
            await drop.ExecuteNonQueryAsync();
        }

        private sealed class StaticTokenCredential : TokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
                => new("unused", DateTimeOffset.MaxValue);

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
                => ValueTask.FromResult(GetToken(requestContext, cancellationToken));
        }

        private sealed class HostingEnvironmentStub(bool isDevelopment) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = isDevelopment ? Environments.Development : Environments.Production;

            public string ApplicationName { get; set; } = "KinHub.Tests";

            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
