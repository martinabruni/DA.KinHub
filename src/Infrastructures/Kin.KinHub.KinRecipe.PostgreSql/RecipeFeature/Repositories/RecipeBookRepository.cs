using Kin.KinHub.KinRecipe.PostgreSql;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.KinRecipe.PostgreSql.RecipeFeature;

public sealed class RecipeBookRepository : PostgreSqlRepository<RecipeBookEntity, RecipeBook, Guid>, IRecipeBookRepository
{
    public RecipeBookRepository(KinRecipeDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<RecipeBook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        return entity?.Adapt<RecipeBook>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecipeBook>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(e => e.FamilyId == familyId && !e.IsDeleted)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<RecipeBook>>();
    }

    /// <inheritdoc/>
    public async Task<RecipeBook> AddAsync(RecipeBook recipeBook, CancellationToken cancellationToken = default)
    {
        var entity = recipeBook.Adapt<RecipeBookEntity>();
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity.Adapt<RecipeBook>();
    }

    /// <inheritdoc/>
    public async Task<RecipeBook> UpdateAsync(RecipeBook recipeBook, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([recipeBook.Id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(RecipeBookEntity), recipeBook.Id);
        Context.Entry(existing).CurrentValues.SetValues(recipeBook.Adapt<RecipeBookEntity>());
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<RecipeBook>();
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([id], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(nameof(RecipeBookEntity), id);
        existing.IsDeleted = true;
        await Context.SaveChangesAsync(cancellationToken);
    }
}
