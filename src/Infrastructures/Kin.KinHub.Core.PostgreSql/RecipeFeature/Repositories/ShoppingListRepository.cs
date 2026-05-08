using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.RecipeFeature;

public sealed class ShoppingListRepository : PostgreSqlRepository<ShoppingListEntity, ShoppingList, Guid>, IShoppingListRepository
{
    public ShoppingListRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ShoppingList>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.FamilyId == familyId)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<ShoppingList>>();
    }

    /// <inheritdoc/>
    public async Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity?.Adapt<ShoppingList>();
    }

    /// <inheritdoc/>
    public async Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default)
    {
        var entity = list.Adapt<ShoppingListEntity>();
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity.Adapt<ShoppingList>();
    }

    /// <inheritdoc/>
    public async Task<ShoppingList> UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([list.Id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(ShoppingListEntity), list.Id);
        Context.Entry(existing).CurrentValues.SetValues(list.Adapt<ShoppingListEntity>());
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<ShoppingList>();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(ShoppingListEntity), id);
        Set.Remove(existing);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
