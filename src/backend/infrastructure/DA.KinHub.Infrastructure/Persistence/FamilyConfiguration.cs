using DA.KinHub.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("families", "shared");
        builder.HasKey(family => family.Id);
        builder.Property(family => family.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(family => family.InactiveAt)
            .HasColumnName("inactive_at");
    }
}
