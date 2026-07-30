using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;
using DA.KinHub.Domain.KinServices;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

public sealed class KinHubDbContext(DbContextOptions<KinHubDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<Family> Families => Set<Family>();

    public DbSet<FamilyMembership> FamilyMemberships => Set<FamilyMembership>();

    public DbSet<KinService> KinServices => Set<KinService>();

    public DbSet<KinServiceLocalization> KinServiceLocalizations => Set<KinServiceLocalization>();

    public DbSet<FamilyKinServiceAvailability> FamilyKinServiceAvailabilities => Set<FamilyKinServiceAvailability>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(KinHubDbContext).Assembly);
}
