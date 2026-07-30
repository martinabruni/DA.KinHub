using DA.KinHub.Domain.KinList;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class KinListCategoryConfiguration : IEntityTypeConfiguration<KinListCategory>
{
    public void Configure(EntityTypeBuilder<KinListCategory> builder)
    {
        builder.ToTable("categories", "kinlist");
        builder.HasKey(category => category.Id);
        builder.HasAlternateKey(category => new { category.Id, category.FamilyId });

        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.FamilyId).HasColumnName("family_id");
        builder.Property(category => category.Name).HasColumnName("name");
        builder.Property(category => category.NormalizedName).HasColumnName("normalized_name");
        builder.Property(category => category.CreatedByApplicationUserId).HasColumnName("created_by_application_user_id");
        builder.Property(category => category.CreatedAt).HasColumnName("created_at");
        builder.Property(category => category.InactiveAt).HasColumnName("inactive_at");

        builder.HasIndex(category => new { category.FamilyId, category.NormalizedName })
            .IsUnique()
            .HasFilter("inactive_at IS NULL");

        builder.HasOne<DA.KinHub.Domain.Families.Family>()
            .WithMany()
            .HasForeignKey(category => category.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DA.KinHub.Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(category => category.CreatedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
