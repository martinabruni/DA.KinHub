using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("families", "shared");
        builder.HasKey(family => family.Id);
        builder.Property(family => family.Name)
            .HasConversion(name => name.Value, value => FamilyName.Create(value))
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(family => family.CreatedByApplicationUserId)
            .HasColumnName("created_by_application_user_id")
            .IsRequired();
        builder.Property(family => family.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(family => family.InactiveAt)
            .HasColumnName("inactive_at");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(family => family.CreatedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
