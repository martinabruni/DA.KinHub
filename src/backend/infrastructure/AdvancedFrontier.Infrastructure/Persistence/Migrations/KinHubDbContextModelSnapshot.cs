using AdvancedFrontier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AdvancedFrontier.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KinHubDbContext))]
partial class KinHubDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0").HasAnnotation("Relational:MaxIdentifierLength", 63);
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("AdvancedFrontier.Domain.Projects.FamilyProject", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            entity.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            entity.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)").HasColumnName("name");
            entity.Property<string>("Stage").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)").HasColumnName("stage");
            entity.HasKey("Id");
            entity.HasIndex("Name").IsUnique();
            entity.ToTable("family_projects");
        });
    }
}
