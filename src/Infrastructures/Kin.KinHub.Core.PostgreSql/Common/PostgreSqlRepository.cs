using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.Common;

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

    public async Task<TDomain> CreateAsync(TDomain model)
    {
        var entity = model.Adapt<TEntity>();
        await OnBeforeCreateAsync(entity);
        await Set.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity.Adapt<TDomain>();
    }

    public async Task<IReadOnlyList<TDomain>> CreateRangeAsync(IReadOnlyCollection<TDomain> models)
    {
        if (models.Count == 0)
        {
            return [];
        }

        var entities = new List<TEntity>(models.Count);
        foreach (var model in models)
        {
            var entity = model.Adapt<TEntity>();
            await OnBeforeCreateAsync(entity);
            entities.Add(entity);
        }

        await Set.AddRangeAsync(entities);
        await Context.SaveChangesAsync();
        return entities.Adapt<IReadOnlyList<TDomain>>();
    }

    public async Task<TDomain> GetAsync(TKey key)
    {
        var entity = await Set.FindAsync(key);
        if (entity is null)
            throw new EntityNotFoundException(typeof(TEntity).Name, key!);
        return entity.Adapt<TDomain>();
    }

    public async Task<IReadOnlyList<TDomain>> GetAllAsync()
    {
        var entities = await Set.ToListAsync();
        return entities.Adapt<IReadOnlyList<TDomain>>();
    }

    public async Task<TDomain> UpdateAsync(TKey key, TDomain model)
    {
        var existing = await Set.FindAsync(key);
        if (existing is null)
            throw new EntityNotFoundException(typeof(TEntity).Name, key!);
        Context.Entry(existing).CurrentValues.SetValues(model.Adapt<TEntity>());
        await Context.SaveChangesAsync();
        return existing.Adapt<TDomain>();
    }

    public async Task<TDomain> DeleteAsync(TKey key)
    {
        var existing = await Set.FindAsync(key);
        if (existing is null)
            throw new EntityNotFoundException(typeof(TEntity).Name, key!);
        Set.Remove(existing);
        await Context.SaveChangesAsync();
        return existing.Adapt<TDomain>();
    }

    protected virtual Task OnBeforeCreateAsync(TEntity entity) => Task.CompletedTask;
}
