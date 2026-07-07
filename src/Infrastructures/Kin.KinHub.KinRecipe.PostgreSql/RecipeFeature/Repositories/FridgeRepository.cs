using Kin.KinHub.KinRecipe.PostgreSql;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.KinRecipe.PostgreSql.RecipeFeature;

public sealed class FridgeRepository : PostgreSqlRepository<FridgeEntity, Fridge, Guid>, IFridgeRepository
{
    public FridgeRepository(KinRecipeDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<Fridge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        return entity?.Adapt<Fridge>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Fridge>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.FamilyId == familyId && !e.IsDeleted)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<Fridge>>();
    }

    /// <inheritdoc/>
    public async Task<Fridge> AddAsync(Fridge fridge, CancellationToken cancellationToken = default)
    {
        var entity = fridge.Adapt<FridgeEntity>();
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity.Adapt<Fridge>();
    }

    /// <inheritdoc/>
    public async Task<Fridge> UpdateAsync(Fridge fridge, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([fridge.Id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(FridgeEntity), fridge.Id);
        Context.Entry(existing).CurrentValues.SetValues(fridge.Adapt<FridgeEntity>());
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<Fridge>();
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(FridgeEntity), id);
        existing.IsDeleted = true;
        await Context.SaveChangesAsync(cancellationToken);
    }
}
