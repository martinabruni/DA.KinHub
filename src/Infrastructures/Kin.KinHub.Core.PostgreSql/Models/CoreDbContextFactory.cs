using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kin.KinHub.Core.PostgreSql.Models;

public sealed class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    public CoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
        var connectionString = ResolveConnectionString();
        optionsBuilder.UseNpgsql(connectionString);

        return new CoreDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString() =>
        Environment.GetEnvironmentVariable("KINHUB_ConnectionStrings__KinHub")
        ?? "Host=localhost;Database=kinhub;Username=postgres;Password=postgres";
}
