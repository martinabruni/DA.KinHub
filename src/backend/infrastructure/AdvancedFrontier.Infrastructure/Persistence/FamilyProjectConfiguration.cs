using AdvancedFrontier.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdvancedFrontier.Infrastructure.Persistence;

internal sealed class FamilyProjectConfiguration : IEntityTypeConfiguration<FamilyProject>
{
    public void Configure(EntityTypeBuilder<FamilyProject> builder)
    {
        builder.ToTable("family_projects");
        builder.HasKey(project => project.Id);
        builder.Property(project => project.Name)
            .HasConversion(name => name.Value, value => new ProjectName(value))
            .HasMaxLength(ProjectName.MaximumLength)
            .HasColumnName("name")
            .IsRequired();
        builder.HasIndex(project => project.Name).IsUnique();
        builder.Property(project => project.Stage).HasConversion<string>().HasMaxLength(32).HasColumnName("stage");
        builder.Property(project => project.CreatedAt).HasColumnName("created_at");
    }
}
