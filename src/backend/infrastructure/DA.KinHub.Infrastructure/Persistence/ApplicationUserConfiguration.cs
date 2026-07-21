using DA.KinHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("application_users", "shared");
        builder.HasKey(applicationUser => applicationUser.Id);
        builder.Property(applicationUser => applicationUser.ExternalIssuer)
            .HasColumnName("external_issuer")
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(applicationUser => applicationUser.ExternalObjectId)
            .HasColumnName("external_object_id")
            .IsRequired();
        builder.Property(applicationUser => applicationUser.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(applicationUser => applicationUser.InactiveAt)
            .HasColumnName("inactive_at");
        builder.HasIndex(applicationUser => new { applicationUser.ExternalIssuer, applicationUser.ExternalObjectId })
            .IsUnique();
    }
}
