using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.KinServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyKinServiceAvailabilityConfiguration : IEntityTypeConfiguration<FamilyKinServiceAvailability>
{
    public void Configure(EntityTypeBuilder<FamilyKinServiceAvailability> builder)
    {
        builder.ToTable("family_kin_service_availabilities", "shared");
        builder.HasKey(availability => availability.Id);
        builder.Property(availability => availability.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();
        builder.Property(availability => availability.KinServiceId)
            .HasColumnName("kin_service_id")
            .IsRequired();
        builder.Property(availability => availability.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(availability => availability.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(availability => availability.UpdatedAt)
            .HasColumnName("updated_at");
        builder.HasIndex(availability => new { availability.FamilyId, availability.KinServiceId })
            .IsUnique();
        builder.HasIndex(availability => availability.FamilyId);
        builder.HasIndex(availability => availability.KinServiceId);
        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(availability => availability.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KinService>()
            .WithMany()
            .HasForeignKey(availability => availability.KinServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
