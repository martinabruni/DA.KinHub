using AdvancedFrontier.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace AdvancedFrontier.Infrastructure.Persistence;

public sealed class KinHubDbContext(DbContextOptions<KinHubDbContext> options) : DbContext(options)
{
    public DbSet<FamilyProject> Projects => Set<FamilyProject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(KinHubDbContext).Assembly);
}
