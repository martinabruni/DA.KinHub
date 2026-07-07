using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.KinRecipe.PostgreSql;

public partial class KinRecipeDbContext : DbContext
{
    public KinRecipeDbContext(DbContextOptions<KinRecipeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<FridgeEntity> FridgeEntity { get; set; }

    public virtual DbSet<FridgeIngredientEntity> FridgeIngredientEntity { get; set; }

    public virtual DbSet<RecipeBookEntity> RecipeBookEntity { get; set; }

    public virtual DbSet<RecipeEntity> RecipeEntity { get; set; }

    public virtual DbSet<RecipeIngredientEntity> RecipeIngredientEntity { get; set; }

    public virtual DbSet<RecipeStepEntity> RecipeStepEntity { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<FridgeEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_kinrecipe_FridgeEntity");
            entity.ToTable("FridgeEntity", "kinrecipe");
            entity.HasIndex(e => e.FamilyId, "IX_kinrecipe_FridgeEntity_FamilyId").HasFilter("(\"IsDeleted\" = false)");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<FridgeIngredientEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_kinrecipe_FridgeIngredientEntity");
            entity.ToTable("FridgeIngredientEntity", "kinrecipe");
            entity.HasIndex(e => e.FridgeId, "IX_kinrecipe_FridgeIngredientEntity_FridgeId").HasFilter("(\"IsDeleted\" = false)");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.MeasureUnit).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Fridge).WithMany(p => p.FridgeIngredientEntity)
                .HasForeignKey(d => d.FridgeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_kinrecipe_FridgeIngredientEntity_FridgeId");
        });

        modelBuilder.Entity<RecipeBookEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_kinrecipe_RecipeBookEntity");
            entity.ToTable("RecipeBookEntity", "kinrecipe");
            entity.HasIndex(e => e.FamilyId, "IX_kinrecipe_RecipeBookEntity_FamilyId").HasFilter("(\"IsDeleted\" = false)");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<RecipeEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_kinrecipe_RecipeEntity");
            entity.ToTable("RecipeEntity", "kinrecipe");
            entity.HasIndex(e => e.RecipeBookId, "IX_kinrecipe_RecipeEntity_RecipeBookId").HasFilter("(\"IsDeleted\" = false)");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Backstory).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.RecipeBook).WithMany(p => p.RecipeEntity)
                .HasForeignKey(d => d.RecipeBookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_kinrecipe_RecipeEntity_RecipeBookId");
        });

        modelBuilder.Entity<RecipeIngredientEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_kinrecipe_RecipeIngredientEntity");
            entity.ToTable("RecipeIngredientEntity", "kinrecipe");
            entity.HasIndex(e => e.RecipeId, "IX_kinrecipe_RecipeIngredientEntity_RecipeId").HasFilter("(\"IsDeleted\" = false)");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.MeasureUnit).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeIngredientEntity)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_kinrecipe_RecipeIngredientEntity_RecipeId");
        });

        modelBuilder.Entity<RecipeStepEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_kinrecipe_RecipeStepEntity");
            entity.ToTable("RecipeStepEntity", "kinrecipe");
            entity.HasIndex(e => e.RecipeId, "IX_kinrecipe_RecipeStepEntity_RecipeId").HasFilter("(\"IsDeleted\" = false)");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeStepEntity)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_kinrecipe_RecipeStepEntity_RecipeId");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
