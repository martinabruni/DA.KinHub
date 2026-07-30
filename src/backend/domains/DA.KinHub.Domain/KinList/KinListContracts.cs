using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinList;

public enum ItemVisibility
{
    Shared = 0,
    Personal = 1
}

public enum ItemStatus
{
    Active = 0,
    Completed = 1
}

public enum ActiveItemsCursorDirection
{
    Next = 0,
    Previous = 1
}

public sealed record ActiveItemsPageAnchor
{
    public ActiveItemsPageAnchor(DateTimeOffset groupCreatedAt, Guid groupId, int positionInGroup, Guid itemId)
    {
        if (groupId == Guid.Empty)
        {
            throw new DomainException("Group ID is required.");
        }

        if (itemId == Guid.Empty)
        {
            throw new DomainException("Item ID is required.");
        }

        if (positionInGroup < 0)
        {
            throw new DomainException("Position in group cannot be negative.");
        }

        GroupCreatedAt = groupCreatedAt;
        GroupId = groupId;
        PositionInGroup = positionInGroup;
        ItemId = itemId;
    }

    public DateTimeOffset GroupCreatedAt { get; }

    public Guid GroupId { get; }

    public int PositionInGroup { get; }

    public Guid ItemId { get; }
}

public sealed record DecodedActiveItemsCursor
{
    public DecodedActiveItemsCursor(ActiveItemsCursorDirection direction, int effectivePageSize, ActiveItemsPageAnchor anchor)
    {
        if (effectivePageSize <= 0)
        {
            throw new DomainException("Effective page size must be positive.");
        }

        Direction = direction;
        EffectivePageSize = effectivePageSize;
        Anchor = anchor;
    }

    public ActiveItemsCursorDirection Direction { get; }

    public int EffectivePageSize { get; }

    public ActiveItemsPageAnchor Anchor { get; }
}

public sealed record ActiveKinListItemCategoryEntry(Guid Id, string Name);

public sealed record ActiveKinListItemEntry(
    Guid Id,
    string Name,
    ActiveItemsPageAnchor Anchor,
    IReadOnlyList<ActiveKinListItemCategoryEntry> Categories,
    int RemainingCategoryCount,
    long Revision);

public sealed record ActiveKinListItemsPage(IReadOnlyList<ActiveKinListItemEntry> Items, bool HasMore);

public interface IActiveKinListItemRepository
{
    Task<ActiveKinListItemsPage> GetActiveItemsPageAsync(
        Guid familyId,
        Guid applicationUserId,
        ActiveItemsCursorDirection direction,
        ActiveItemsPageAnchor? anchor,
        int effectivePageSize,
        CancellationToken cancellationToken);
}

public interface IActiveItemsCursorCodec
{
    string Encode(Guid familyId, Guid applicationUserId, ActiveItemsCursorDirection direction, int effectivePageSize, ActiveItemsPageAnchor anchor);

    DecodedActiveItemsCursor Decode(string opaqueCursor, Guid familyId, Guid applicationUserId);
}

public sealed class ActiveItemsCursorInvalidException(string message, Exception? innerException = null) : Exception(message, innerException);
