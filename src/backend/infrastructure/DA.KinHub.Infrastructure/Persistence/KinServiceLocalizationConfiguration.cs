using DA.KinHub.Domain.KinServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class KinServiceLocalizationConfiguration : IEntityTypeConfiguration<KinServiceLocalization>
{
    public void Configure(EntityTypeBuilder<KinServiceLocalization> builder)
    {
        builder.ToTable("kin_service_localizations", "shared");
        builder.HasKey(localization => localization.Id);
        builder.Property(localization => localization.KinServiceId)
            .HasColumnName("kin_service_id")
            .IsRequired();
        builder.Property(localization => localization.Language)
            .HasColumnName("language")
            .HasMaxLength(8)
            .IsRequired();
        builder.Property(localization => localization.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(localization => localization.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(localization => localization.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(localization => localization.UpdatedAt)
            .HasColumnName("updated_at");
        builder.HasIndex(localization => new { localization.KinServiceId, localization.Language })
            .IsUnique();
        builder.HasOne<KinService>()
            .WithMany()
            .HasForeignKey(localization => localization.KinServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
