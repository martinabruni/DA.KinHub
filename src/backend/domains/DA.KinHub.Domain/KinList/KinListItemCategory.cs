using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinList;

public sealed class KinListItemCategory
{
    private KinListItemCategory()
    {
    }

    private KinListItemCategory(Guid familyId, Guid itemId, Guid categoryId)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainException("Family ID is required.");
        }

        if (itemId == Guid.Empty)
        {
            throw new DomainException("Item ID is required.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category ID is required.");
        }

        FamilyId = familyId;
        ItemId = itemId;
        CategoryId = categoryId;
    }

    public Guid FamilyId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid CategoryId { get; private set; }

    public static KinListItemCategory Create(Guid familyId, Guid itemId, Guid categoryId) => new(familyId, itemId, categoryId);
}
