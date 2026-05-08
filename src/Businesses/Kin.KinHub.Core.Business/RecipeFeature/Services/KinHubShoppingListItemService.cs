using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class KinHubShoppingListItemService : IShoppingListItemService
{
    private readonly IShoppingListItemRepository _itemRepository;
    private readonly IShoppingListRepository _listRepository;
    private readonly IFamilyRepository _familyRepository;

    public KinHubShoppingListItemService(
        IShoppingListItemRepository itemRepository,
        IShoppingListRepository listRepository,
        IFamilyRepository familyRepository)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _familyRepository = familyRepository;
    }

    public async Task<Result<IReadOnlyList<ShoppingListItemResponse>>> GetAllByListIdAsync(
        Guid listId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<IReadOnlyList<ShoppingListItemResponse>>.NotFound("Family not found for the current user.");

        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null)
            return Result<IReadOnlyList<ShoppingListItemResponse>>.NotFound("Shopping list not found.");
        if (list.FamilyId != family.Id)
            return Result<IReadOnlyList<ShoppingListItemResponse>>.Unauthorized("Access denied.");

        var items = await _itemRepository.GetAllByListIdAsync(listId, cancellationToken);
        return Result<IReadOnlyList<ShoppingListItemResponse>>.Success(items.Select(Map).ToList());
    }

    public async Task<Result<ShoppingListItemResponse>> AddAsync(
        Guid listId,
        CreateShoppingListItemRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<ShoppingListItemResponse>.NotFound("Family not found for the current user.");

        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null)
            return Result<ShoppingListItemResponse>.NotFound("Shopping list not found.");
        if (list.FamilyId != family.Id)
            return Result<ShoppingListItemResponse>.Unauthorized("Access denied.");

        var now = DateTime.UtcNow;
        var item = new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            IsChecked = false,
            ShoppingListId = listId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _itemRepository.AddAsync(item, cancellationToken);
        await BumpListUpdatedAtAsync(list, cancellationToken);
        return Result<ShoppingListItemResponse>.Success(Map(created));
    }

    public async Task<Result<BulkAddShoppingListItemsResponse>> BulkAddAsync(
        Guid listId,
        BulkAddShoppingListItemsRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<BulkAddShoppingListItemsResponse>.NotFound("Family not found for the current user.");

        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null)
            return Result<BulkAddShoppingListItemsResponse>.NotFound("Shopping list not found.");
        if (list.FamilyId != family.Id)
            return Result<BulkAddShoppingListItemsResponse>.Unauthorized("Access denied.");

        var now = DateTime.UtcNow;
        var items = request.Names.Select(name => new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsChecked = false,
            ShoppingListId = listId,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        var addedCount = await _itemRepository.AddBulkAsync(items, cancellationToken);
        if (addedCount > 0)
            await BumpListUpdatedAtAsync(list, cancellationToken);

        return Result<BulkAddShoppingListItemsResponse>.Success(new BulkAddShoppingListItemsResponse { AddedCount = addedCount });
    }

    public async Task<Result<ShoppingListItemResponse>> ToggleCheckedAsync(
        Guid listId,
        Guid itemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<ShoppingListItemResponse>.NotFound("Family not found for the current user.");

        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null)
            return Result<ShoppingListItemResponse>.NotFound("Shopping list not found.");
        if (list.FamilyId != family.Id)
            return Result<ShoppingListItemResponse>.Unauthorized("Access denied.");

        var updated = await _itemRepository.ToggleCheckedAsync(itemId, cancellationToken);
        await BumpListUpdatedAtAsync(list, cancellationToken);
        return Result<ShoppingListItemResponse>.Success(Map(updated));
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid listId,
        Guid itemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<bool>.NotFound("Family not found for the current user.");

        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null)
            return Result<bool>.NotFound("Shopping list not found.");
        if (list.FamilyId != family.Id)
            return Result<bool>.Unauthorized("Access denied.");

        await _itemRepository.DeleteAsync(itemId, cancellationToken);
        await BumpListUpdatedAtAsync(list, cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteCheckedAsync(
        Guid listId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<bool>.NotFound("Family not found for the current user.");

        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null)
            return Result<bool>.NotFound("Shopping list not found.");
        if (list.FamilyId != family.Id)
            return Result<bool>.Unauthorized("Access denied.");

        await _itemRepository.DeleteCheckedByListIdAsync(listId, cancellationToken);
        await BumpListUpdatedAtAsync(list, cancellationToken);
        return Result<bool>.Success(true);
    }

    private async Task BumpListUpdatedAtAsync(ShoppingList list, CancellationToken cancellationToken)
    {
        list.UpdatedAt = DateTime.UtcNow;
        await _listRepository.UpdateAsync(list, cancellationToken);
    }

    private static ShoppingListItemResponse Map(ShoppingListItem item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            IsChecked = item.IsChecked,
            CreatedAt = item.CreatedAt,
        };
}

