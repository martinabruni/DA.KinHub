using DA.KinHub.Domain.KinList;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class RegistrationGroupConfiguration : IEntityTypeConfiguration<RegistrationGroup>
{
    public void Configure(EntityTypeBuilder<RegistrationGroup> builder)
    {
        builder.ToTable("registration_groups", "kinlist");
        builder.HasKey(group => group.Id);
        builder.HasAlternateKey(group => new { group.Id, group.FamilyId });

        builder.Property(group => group.Id).ValueGeneratedNever();
        builder.Property(group => group.FamilyId).HasColumnName("family_id");
        builder.Property(group => group.RecordingId).HasColumnName("recording_id");
        builder.Property(group => group.CreatedByApplicationUserId).HasColumnName("created_by_application_user_id");
        builder.Property(group => group.CreatedAt).HasColumnName("created_at");
        builder.Property(group => group.InactiveAt).HasColumnName("inactive_at");

        builder.HasIndex(group => new { group.FamilyId, group.RecordingId }).IsUnique();
        builder.HasIndex(group => new { group.FamilyId, group.CreatedAt, group.Id }).IsDescending(false, true, true);

        builder.HasOne<DA.KinHub.Domain.Families.Family>()
            .WithMany()
            .HasForeignKey(group => group.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DA.KinHub.Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(group => group.CreatedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
