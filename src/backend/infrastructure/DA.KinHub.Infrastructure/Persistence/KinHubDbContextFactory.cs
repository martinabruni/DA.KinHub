using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DA.KinHub.Infrastructure.Persistence;

public sealed class KinHubDbContextFactory : IDesignTimeDbContextFactory<KinHubDbContext>
{
    public KinHubDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("Database__ConnectionString")
            ?? "Host=localhost;Port=5432;Database=kinhub;Username=kinhub;Password=kinhub";
        var options = new DbContextOptionsBuilder<KinHubDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(KinHubDbContext).Assembly.FullName))
            .Options;
        return new KinHubDbContext(options);
    }
}
