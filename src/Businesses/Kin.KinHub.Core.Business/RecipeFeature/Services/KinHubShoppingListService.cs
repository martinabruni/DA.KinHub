using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class KinHubShoppingListService : IShoppingListService
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IShoppingListItemRepository _shoppingListItemRepository;
    private readonly IFamilyRepository _familyRepository;

    public KinHubShoppingListService(
        IShoppingListRepository shoppingListRepository,
        IShoppingListItemRepository shoppingListItemRepository,
        IFamilyRepository familyRepository)
    {
        _shoppingListRepository = shoppingListRepository;
        _shoppingListItemRepository = shoppingListItemRepository;
        _familyRepository = familyRepository;
    }

    public async Task<Result<IReadOnlyList<ShoppingListResponse>>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<IReadOnlyList<ShoppingListResponse>>.NotFound("Family not found for the current user.");

        var lists = await _shoppingListRepository.GetAllByFamilyIdAsync(family.Id, cancellationToken);

        var responses = new List<ShoppingListResponse>();
        foreach (var list in lists)
        {
            var items = await _shoppingListItemRepository.GetAllByListIdAsync(list.Id, cancellationToken);
            responses.Add(MapWithCounts(list, items));
        }

        return Result<IReadOnlyList<ShoppingListResponse>>.Success(responses);
    }

    public async Task<Result<ShoppingListResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<ShoppingListResponse>.NotFound("Family not found for the current user.");

        var list = await _shoppingListRepository.GetByIdAsync(id, cancellationToken);
        if (list is null)
            return Result<ShoppingListResponse>.NotFound("Shopping list not found.");
        if (list.FamilyId != family.Id)
            return Result<ShoppingListResponse>.Unauthorized("Access denied.");

        var items = await _shoppingListItemRepository.GetAllByListIdAsync(list.Id, cancellationToken);
        return Result<ShoppingListResponse>.Success(MapWithCounts(list, items));
    }

    public async Task<Result<ShoppingListResponse>> CreateAsync(
        CreateShoppingListRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<ShoppingListResponse>.NotFound("Family not found for the current user.");

        var now = DateTime.UtcNow;
        var list = new ShoppingList
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            FamilyId = family.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _shoppingListRepository.AddAsync(list, cancellationToken);
        return Result<ShoppingListResponse>.Success(MapWithCounts(created, []));
    }

    public async Task<Result<ShoppingListResponse>> UpdateAsync(
        Guid id,
        UpdateShoppingListRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<ShoppingListResponse>.NotFound("Family not found for the current user.");

        var list = await _shoppingListRepository.GetByIdAsync(id, cancellationToken);
        if (list is null)
            return Result<ShoppingListResponse>.NotFound("Shopping list not found.");
        if (list.FamilyId != family.Id)
            return Result<ShoppingListResponse>.Unauthorized("Access denied.");

        list.Name = request.Name;
        list.UpdatedAt = DateTime.UtcNow;

        var updated = await _shoppingListRepository.UpdateAsync(list, cancellationToken);
        var items = await _shoppingListItemRepository.GetAllByListIdAsync(updated.Id, cancellationToken);
        return Result<ShoppingListResponse>.Success(MapWithCounts(updated, items));
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<bool>.NotFound("Family not found for the current user.");

        var list = await _shoppingListRepository.GetByIdAsync(id, cancellationToken);
        if (list is null)
            return Result<bool>.NotFound("Shopping list not found.");
        if (list.FamilyId != family.Id)
            return Result<bool>.Unauthorized("Access denied.");

        await _shoppingListRepository.DeleteAsync(id, cancellationToken);
        return Result<bool>.Success(true);
    }

    private static ShoppingListResponse MapWithCounts(ShoppingList list, IReadOnlyList<ShoppingListItem> items) =>
        new()
        {
            Id = list.Id,
            Name = list.Name,
            FamilyId = list.FamilyId,
            ItemCount = items.Count,
            CheckedCount = items.Count(i => i.IsChecked),
        };
}
