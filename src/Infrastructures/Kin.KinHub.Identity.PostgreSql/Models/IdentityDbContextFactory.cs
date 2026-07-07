using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kin.KinHub.Identity.PostgreSql;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        var connectionString = ResolveConnectionString();
        optionsBuilder.UseNpgsql(connectionString);

        return new IdentityDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString() =>
        Environment.GetEnvironmentVariable("KINHUB_ConnectionStrings__KinHub")
        ?? "Host=localhost;Database=kinhub;Username=postgres;Password=postgres";
}
