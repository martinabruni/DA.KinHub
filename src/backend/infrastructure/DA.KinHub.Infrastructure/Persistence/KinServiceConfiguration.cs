using DA.KinHub.Domain.KinServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class KinServiceConfiguration : IEntityTypeConfiguration<KinService>
{
    public void Configure(EntityTypeBuilder<KinService> builder)
    {
        builder.ToTable("kin_services", "shared");
        builder.HasKey(service => service.Id);
        builder.Property(service => service.Key)
            .HasColumnName("key")
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(service => service.Route)
            .HasColumnName("route")
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(service => service.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(service => service.IsPreconfigured)
            .HasColumnName("is_preconfigured")
            .IsRequired();
        builder.Property(service => service.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(service => service.UpdatedAt)
            .HasColumnName("updated_at");
        builder.HasIndex(service => service.Key)
            .IsUnique();
        builder.HasIndex(service => service.Route)
            .IsUnique();
    }
}
