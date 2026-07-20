using AdvancedFrontier.Domain.Families;
using AdvancedFrontier.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdvancedFrontier.Infrastructure.Persistence;

internal sealed class FamilyMembershipConfiguration : IEntityTypeConfiguration<FamilyMembership>
{
    public void Configure(EntityTypeBuilder<FamilyMembership> builder)
    {
        builder.ToTable("family_memberships", "shared");
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.ApplicationUserId)
            .HasColumnName("application_user_id")
            .IsRequired();
        builder.Property(membership => membership.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();
        builder.Property(membership => membership.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(membership => membership.InactiveAt)
            .HasColumnName("inactive_at");
        builder.HasIndex(membership => new { membership.ApplicationUserId, membership.FamilyId })
            .IsUnique();
        builder.HasIndex(membership => membership.ApplicationUserId)
            .HasDatabaseName("IX_family_memberships_single_active_user")
            .HasFilter("inactive_at IS NULL")
            .IsUnique();
        builder.HasIndex(membership => new { membership.ApplicationUserId, membership.FamilyId, membership.InactiveAt });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(membership => membership.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(membership => membership.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
