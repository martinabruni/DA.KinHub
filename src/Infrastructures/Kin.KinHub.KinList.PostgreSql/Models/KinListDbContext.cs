using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.KinList.PostgreSql.Models;

public sealed class KinListDbContext : DbContext
{
    public KinListDbContext(DbContextOptions<KinListDbContext> options)
        : base(options)
    {
    }

    public DbSet<KinListEntity> Lists => Set<KinListEntity>();
    public DbSet<KinListItemEntity> Items => Set<KinListItemEntity>();
    public DbSet<IdempotencyRecordEntity> IdempotencyRecords => Set<IdempotencyRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("kinlist");

        modelBuilder.Entity<KinListEntity>(entity =>
        {
            entity.ToTable("List");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.FamilyId, x.IsDeleted, x.LastModifiedAt });
            entity.Property(x => x.Title).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Version).IsRequired();
        });

        modelBuilder.Entity<KinListItemEntity>(entity =>
        {
            entity.ToTable("ListItem");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ListId, x.IsDeleted, x.IsCompleted, x.ActivationOrder });
            entity.Property(x => x.Text).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.List)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdempotencyRecordEntity>(entity =>
        {
            entity.ToTable("IdempotencyRecord");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Key, x.FamilyId, x.UserId }).IsUnique();
            entity.Property(x => x.Key).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ResponseJson).HasColumnType("text").IsRequired();
        });
    }
}
