using DA.KinHub.Domain.KinList;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class KinListItemCategoryConfiguration : IEntityTypeConfiguration<KinListItemCategory>
{
    public void Configure(EntityTypeBuilder<KinListItemCategory> builder)
    {
        builder.ToTable("item_categories", "kinlist");
        builder.HasKey(itemCategory => new { itemCategory.ItemId, itemCategory.CategoryId });

        builder.Property(itemCategory => itemCategory.FamilyId).HasColumnName("family_id");
        builder.Property(itemCategory => itemCategory.ItemId).HasColumnName("item_id");
        builder.Property(itemCategory => itemCategory.CategoryId).HasColumnName("category_id");

        builder.HasIndex(itemCategory => new { itemCategory.FamilyId, itemCategory.ItemId });
        builder.HasIndex(itemCategory => new { itemCategory.FamilyId, itemCategory.CategoryId, itemCategory.ItemId });

        builder.HasOne<KinListItem>()
            .WithMany()
            .HasForeignKey(itemCategory => new { itemCategory.ItemId, itemCategory.FamilyId })
            .HasPrincipalKey(item => new { item.Id, item.FamilyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<KinListCategory>()
            .WithMany()
            .HasForeignKey(itemCategory => new { itemCategory.CategoryId, itemCategory.FamilyId })
            .HasPrincipalKey(category => new { category.Id, category.FamilyId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
