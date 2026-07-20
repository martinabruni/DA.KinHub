using AdvancedFrontier.Domain.Families;
using AdvancedFrontier.Domain.Identity;
using AdvancedFrontier.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace AdvancedFrontier.Infrastructure.Persistence;

public sealed class KinHubDbContext(DbContextOptions<KinHubDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<Family> Families => Set<Family>();

    public DbSet<FamilyMembership> FamilyMemberships => Set<FamilyMembership>();

    public DbSet<FamilyProject> Projects => Set<FamilyProject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(KinHubDbContext).Assembly);
}
