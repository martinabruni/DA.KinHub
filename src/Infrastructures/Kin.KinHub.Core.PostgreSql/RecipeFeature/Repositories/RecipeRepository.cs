using Kin.KinHub.Core.PostgreSql;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.RecipeFeature;

public sealed class RecipeRepository : PostgreSqlRepository<RecipeEntity, Recipe, Guid>, IRecipeRepository
{
    public RecipeRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        return entity?.Adapt<Recipe>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Recipe>> GetAllByRecipeBookIdAsync(Guid recipeBookId, CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.RecipeBookId == recipeBookId && !e.IsDeleted)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<Recipe>>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Recipe>> GetAllByRecipeBookIdsAsync(IReadOnlyCollection<Guid> recipeBookIds, CancellationToken cancellationToken = default)
    {
        if (recipeBookIds.Count == 0)
        {
            return [];
        }

        var entities = await Set
            .Where(e => recipeBookIds.Contains(e.RecipeBookId) && !e.IsDeleted)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<Recipe>>();
    }

    /// <inheritdoc/>
    public async Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        var entity = recipe.Adapt<RecipeEntity>();
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity.Adapt<Recipe>();
    }

    /// <inheritdoc/>
    public async Task<Recipe> UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([recipe.Id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(RecipeEntity), recipe.Id);
        Context.Entry(existing).CurrentValues.SetValues(recipe.Adapt<RecipeEntity>());
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<Recipe>();
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(RecipeEntity), id);
        existing.IsDeleted = true;
        await Context.SaveChangesAsync(cancellationToken);
    }
}
