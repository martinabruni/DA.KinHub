using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.RecipeFeature;

public sealed class ShoppingListItemRepository : PostgreSqlRepository<ShoppingListItemEntity, ShoppingListItem, Guid>, IShoppingListItemRepository
{
    public ShoppingListItemRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ShoppingListItem>> GetAllByListIdAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.ShoppingListId == listId)
            .OrderBy(e => e.IsChecked)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<ShoppingListItem>>();
    }

    /// <inheritdoc/>
    public async Task<ShoppingListItem> AddAsync(ShoppingListItem item, CancellationToken cancellationToken = default)
    {
        var entity = item.Adapt<ShoppingListItemEntity>();
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity.Adapt<ShoppingListItem>();
    }

    /// <inheritdoc/>
    public async Task<int> AddBulkAsync(IEnumerable<ShoppingListItem> items, CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return 0;

        var listId = itemList[0].ShoppingListId;
        var existingNames = await Set
            .Where(e => e.ShoppingListId == listId)
            .Select(e => e.Name.ToLower())
            .ToListAsync(cancellationToken);

        var toInsert = itemList
            .Where(i => !existingNames.Contains(i.Name.ToLower()))
            .Select(i => i.Adapt<ShoppingListItemEntity>())
            .ToList();

        if (toInsert.Count == 0) return 0;

        await Set.AddRangeAsync(toInsert, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return toInsert.Count;
    }

    /// <inheritdoc/>
    public async Task<ShoppingListItem> ToggleCheckedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(ShoppingListItemEntity), id);
        existing.IsChecked = !existing.IsChecked;
        existing.UpdatedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<ShoppingListItem>();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(ShoppingListItemEntity), id);
        Set.Remove(existing);
        await Context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteCheckedByListIdAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var checked_ = await Set
            .Where(e => e.ShoppingListId == listId && e.IsChecked)
            .ToListAsync(cancellationToken);
        if (checked_.Count > 0)
        {
            Set.RemoveRange(checked_);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(Guid listId, string name, CancellationToken cancellationToken = default)
    {
        return await Set.AnyAsync(
            e => e.ShoppingListId == listId && e.Name.ToLower() == name.ToLower(),
            cancellationToken);
    }
}
