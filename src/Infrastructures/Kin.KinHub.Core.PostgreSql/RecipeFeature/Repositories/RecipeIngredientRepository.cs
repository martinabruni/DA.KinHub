using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.RecipeFeature;

public sealed class RecipeIngredientRepository : PostgreSqlRepository<RecipeIngredientEntity, RecipeIngredient, Guid>, IRecipeIngredientRepository
{
    public RecipeIngredientRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<RecipeIngredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        return entity?.Adapt<RecipeIngredient>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecipeIngredient>> GetAllByFamilyIdAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.RecipeId == recipeId && !e.IsDeleted)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<RecipeIngredient>>();
    }

    /// <inheritdoc/>
    public async Task<RecipeIngredient> AddAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        var entity = ingredient.Adapt<RecipeIngredientEntity>();
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity.Adapt<RecipeIngredient>();
    }

    /// <inheritdoc/>
    public async Task<RecipeIngredient> UpdateAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([ingredient.Id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(RecipeIngredientEntity), ingredient.Id);
        Context.Entry(existing).CurrentValues.SetValues(ingredient.Adapt<RecipeIngredientEntity>());
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<RecipeIngredient>();
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(RecipeIngredientEntity), id);
        existing.IsDeleted = true;
        await Context.SaveChangesAsync(cancellationToken);
    }
}
