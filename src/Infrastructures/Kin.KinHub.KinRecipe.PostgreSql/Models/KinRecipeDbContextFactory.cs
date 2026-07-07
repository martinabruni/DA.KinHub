using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kin.KinHub.KinRecipe.PostgreSql;

public sealed class KinRecipeDbContextFactory : IDesignTimeDbContextFactory<KinRecipeDbContext>
{
    public KinRecipeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("KINHUB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=kinhub;Username=postgres;Password=postgres";
        var optionsBuilder = new DbContextOptionsBuilder<KinRecipeDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new KinRecipeDbContext(optionsBuilder.Options);
    }
}
