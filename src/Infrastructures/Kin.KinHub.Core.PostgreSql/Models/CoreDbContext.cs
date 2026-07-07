using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql;

public partial class CoreDbContext : DbContext
{
    public CoreDbContext(DbContextOptions<CoreDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<FamilyEntity> FamilyEntity { get; set; }

    public virtual DbSet<FamilyMemberEntity> FamilyMemberEntity { get; set; }

    public virtual DbSet<FamilyServiceEntity> FamilyServiceEntity { get; set; }

    public virtual DbSet<KinHubServiceEntity> KinHubServiceEntity { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<FamilyEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_core_FamilyEntity");
            entity.ToTable("FamilyEntity", "core");
            entity.HasIndex(e => e.UserId, "IX_core_FamilyEntity_UserId");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<FamilyMemberEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_core_FamilyMemberEntity");
            entity.ToTable("FamilyMemberEntity", "core");
            entity.HasIndex(e => e.FamilyId, "IX_core_FamilyMemberEntity_FamilyId");
            entity.HasIndex(e => new { e.FamilyId, e.Name }, "UQ_core_FamilyMemberEntity_FamilyName").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Family).WithMany(p => p.FamilyMemberEntity)
                .HasForeignKey(d => d.FamilyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_core_FamilyMemberEntity_FamilyId");
        });

        modelBuilder.Entity<FamilyServiceEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_core_FamilyServiceEntity");
            entity.ToTable("FamilyServiceEntity", "core");
            entity.HasIndex(e => e.FamilyId, "IX_core_FamilyServiceEntity_FamilyId");
            entity.HasIndex(e => new { e.FamilyId, e.ServiceId }, "UQ_core_FamilyServiceEntity_FamilyService").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Family).WithMany(p => p.FamilyServiceEntity)
                .HasForeignKey(d => d.FamilyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_core_FamilyServiceEntity_FamilyId");
            entity.HasOne(d => d.Service).WithMany(p => p.FamilyServiceEntity)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_core_FamilyServiceEntity_ServiceId");
        });

        modelBuilder.Entity<KinHubServiceEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_core_KinHubServiceEntity");
            entity.ToTable("KinHubServiceEntity", "core");
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BaseUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
