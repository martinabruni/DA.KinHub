using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyInvitationConfiguration : IEntityTypeConfiguration<FamilyInvitation>
{
    public void Configure(EntityTypeBuilder<FamilyInvitation> builder)
    {
        builder.ToTable("family_invitations", "shared", table =>
        {
            table.HasCheckConstraint("CK_family_invitations_expires_after_created", "expires_at > created_at");
            table.HasCheckConstraint("CK_family_invitations_revoked_after_created", "revoked_at IS NULL OR revoked_at >= created_at");
            table.HasCheckConstraint("CK_family_invitations_consumed_after_created", "consumed_at IS NULL OR consumed_at >= created_at");
            table.HasCheckConstraint("CK_family_invitations_hmac_non_empty", "octet_length(code_hmac) > 0");
            table.HasCheckConstraint("CK_family_invitations_hmac_key_version_non_empty", "char_length(hmac_key_version) > 0");
        });
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();
        builder.Property(invitation => invitation.CreatedByApplicationUserId)
            .HasColumnName("created_by_application_user_id")
            .IsRequired();
        builder.Property(invitation => invitation.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(invitation => invitation.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();
        builder.Property(invitation => invitation.CodeHmac)
            .HasColumnName("code_hmac")
            .HasColumnType("bytea")
            .IsRequired();
        builder.Property(invitation => invitation.HmacKeyVersion)
            .HasColumnName("hmac_key_version")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(invitation => invitation.RevokedAt)
            .HasColumnName("revoked_at");
        builder.Property(invitation => invitation.ConsumedAt)
            .HasColumnName("consumed_at");
        builder.HasIndex(invitation => new { invitation.FamilyId, invitation.CreatedAt, invitation.Id })
            .HasDatabaseName("IX_family_invitations_active_by_family_created_at_id")
            .HasFilter("revoked_at IS NULL AND consumed_at IS NULL");
        builder.HasIndex(invitation => invitation.CreatedByApplicationUserId);
        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(invitation => invitation.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(invitation => invitation.CreatedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
