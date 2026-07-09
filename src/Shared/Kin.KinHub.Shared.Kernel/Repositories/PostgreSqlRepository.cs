namespace Kin.KinHub.Shared.Kernel.Repositories;

using Kin.KinHub.Shared.Kernel.Exceptions;
using Mapster;
using Microsoft.EntityFrameworkCore;

public abstract class PostgreSqlRepository<TEntity, TDomain, TKey>
    where TEntity : class
    where TDomain : class
{
    protected DbContext Context { get; }
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    protected PostgreSqlRepository(DbContext context)
    {
        Context = context;
    }

    public async Task<TDomain> CreateAsync(TDomain model, CancellationToken cancellationToken = default)
    {
        var entity = model.Adapt<TEntity>();
        await OnBeforeCreateAsync(entity, cancellationToken);
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity.Adapt<TDomain>();
    }

    public async Task<IReadOnlyList<TDomain>> CreateRangeAsync(
        IReadOnlyCollection<TDomain> models,
        CancellationToken cancellationToken = default)
    {
        if (models.Count == 0)
        {
            return [];
        }

        var entities = new List<TEntity>(models.Count);
        foreach (var model in models)
        {
            var entity = model.Adapt<TEntity>();
            await OnBeforeCreateAsync(entity, cancellationToken);
            entities.Add(entity);
        }

        await Set.AddRangeAsync(entities, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<TDomain>>();
    }

    public async Task<TDomain> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        var entity = await Set.FindAsync([key], cancellationToken);
        if (entity is null)
            throw new EntityNotFoundException(typeof(TEntity).Name, key!);
        return entity.Adapt<TDomain>();
    }

    public async Task<IReadOnlyList<TDomain>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await Set.ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<TDomain>>();
    }

    public async Task<TDomain> UpdateAsync(TKey key, TDomain model, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([key], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(typeof(TEntity).Name, key!);
        Context.Entry(existing).CurrentValues.SetValues(model.Adapt<TEntity>());
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<TDomain>();
    }

    public async Task<TDomain> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([key], cancellationToken);
        if (existing is null)
            throw new EntityNotFoundException(typeof(TEntity).Name, key!);
        Set.Remove(existing);
        await Context.SaveChangesAsync(cancellationToken);
        return existing.Adapt<TDomain>();
    }

    protected virtual Task OnBeforeCreateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;
}
