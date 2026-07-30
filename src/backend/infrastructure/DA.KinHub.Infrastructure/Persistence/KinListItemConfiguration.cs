using DA.KinHub.Domain.KinList;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class KinListItemConfiguration : IEntityTypeConfiguration<KinListItem>
{
    public void Configure(EntityTypeBuilder<KinListItem> builder)
    {
        builder.ToTable("items", "kinlist");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.Id, item.FamilyId });

        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.FamilyId).HasColumnName("family_id");
        builder.Property(item => item.RegistrationGroupId).HasColumnName("registration_group_id");
        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasConversion(name => name.Value, value => KinListItemName.Create(value));
        builder.Property(item => item.PositionInGroup).HasColumnName("position_in_group");
        builder.Property(item => item.OwnerApplicationUserId).HasColumnName("owner_application_user_id");
        builder.Property(item => item.Visibility).HasColumnName("visibility").HasConversion<string>();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.ModifiedByApplicationUserId).HasColumnName("modified_by_application_user_id");
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        builder.Property(item => item.CompletedByApplicationUserId).HasColumnName("completed_by_application_user_id");
        builder.Property(item => item.CompletedAt).HasColumnName("completed_at");
        builder.Property(item => item.InactiveAt).HasColumnName("inactive_at");
        builder.Property(item => item.Revision).HasColumnName("revision").IsConcurrencyToken();

        builder.HasIndex(item => new { item.RegistrationGroupId, item.PositionInGroup }).IsUnique();
        builder.HasIndex(item => new { item.RegistrationGroupId, item.PositionInGroup, item.Id })
            .HasDatabaseName("IX_items_shared_active")
            .HasFilter("inactive_at IS NULL AND status = 'Active' AND visibility = 'Shared'");
        builder.HasIndex(item => new { item.RegistrationGroupId, item.OwnerApplicationUserId, item.PositionInGroup, item.Id })
            .HasDatabaseName("IX_items_personal_active")
            .HasFilter("inactive_at IS NULL AND status = 'Active' AND visibility = 'Personal'");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_items_position_in_group_non_negative", "position_in_group >= 0");
            table.HasCheckConstraint("CK_items_revision_positive", "revision >= 1");
            table.HasCheckConstraint("CK_items_visibility", "visibility IN ('Shared', 'Personal')");
            table.HasCheckConstraint("CK_items_status", "status IN ('Active', 'Completed')");
        });

        builder.HasOne<RegistrationGroup>()
            .WithMany()
            .HasForeignKey(item => new { item.RegistrationGroupId, item.FamilyId })
            .HasPrincipalKey(group => new { group.Id, group.FamilyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DA.KinHub.Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.OwnerApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DA.KinHub.Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.ModifiedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DA.KinHub.Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.CompletedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
