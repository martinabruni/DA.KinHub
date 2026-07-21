using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

public sealed class KinHubDbContext(DbContextOptions<KinHubDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<Family> Families => Set<Family>();

    public DbSet<FamilyMembership> FamilyMemberships => Set<FamilyMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(KinHubDbContext).Assembly);
}
