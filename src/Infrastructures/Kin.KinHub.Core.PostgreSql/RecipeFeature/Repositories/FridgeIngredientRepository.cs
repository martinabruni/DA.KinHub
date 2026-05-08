using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.RecipeFeature;

public sealed class FridgeIngredientRepository : PostgreSqlRepository<FridgeIngredientEntity, FridgeIngredient, Guid>, IFridgeIngredientRepository
{
    public FridgeIngredientRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<FridgeIngredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        return entity?.Adapt<FridgeIngredient>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FridgeIngredient>> GetAllByFamilyIdAsync(Guid fridgeId, CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.FridgeId == fridgeId && !e.IsDeleted)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<FridgeIngredient>>();
    }

    /// <inheritdoc/>
    public async Task<FridgeIngredient> AddAsync(FridgeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        var entity = ingredient.Adapt<FridgeIngredientEntity>();
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity.Adapt<FridgeIngredient>();
    }

    /// <inheritdoc/>
    public async Task<FridgeIngredient> UpdateAsync(FridgeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([ingredient.Id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(FridgeIngredientEntity), ingredient.Id);
        Context.Entry(existing).CurrentValues.SetValues(ingredient.Adapt<FridgeIngredientEntity>());
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<FridgeIngredient>();
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(FridgeIngredientEntity), id);
        existing.IsDeleted = true;
        await Context.SaveChangesAsync(cancellationToken);
    }
}
