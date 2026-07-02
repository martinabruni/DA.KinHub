using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kin.KinHub.KinList.PostgreSql.Models;

public sealed class KinListDbContextFactory : IDesignTimeDbContextFactory<KinListDbContext>
{
    public KinListDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("KINHUB_CONNECTIONSTRINGS__KINHUB")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__KinHub")
            ?? "Host=localhost;Port=5432;Database=kinhub;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<KinListDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new KinListDbContext(optionsBuilder.Options);
    }
}
