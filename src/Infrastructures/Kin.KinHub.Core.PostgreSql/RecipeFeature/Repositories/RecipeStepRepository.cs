using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.RecipeFeature;

public sealed class RecipeStepRepository : PostgreSqlRepository<RecipeStepEntity, RecipeStep, Guid>, IRecipeStepRepository
{
    public RecipeStepRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<RecipeStep?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        return entity?.Adapt<RecipeStep>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecipeStep>> GetAllByFamilyIdAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.RecipeId == recipeId && !e.IsDeleted)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<RecipeStep>>();
    }

    /// <inheritdoc/>
    public async Task<RecipeStep> AddAsync(RecipeStep step, CancellationToken cancellationToken = default)
    {
        var entity = step.Adapt<RecipeStepEntity>();
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity.Adapt<RecipeStep>();
    }

    /// <inheritdoc/>
    public async Task<RecipeStep> UpdateAsync(RecipeStep step, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([step.Id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(RecipeStepEntity), step.Id);
        Context.Entry(existing).CurrentValues.SetValues(step.Adapt<RecipeStepEntity>());
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<RecipeStep>();
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(RecipeStepEntity), id);
        existing.IsDeleted = true;
        await Context.SaveChangesAsync(cancellationToken);
    }
}
