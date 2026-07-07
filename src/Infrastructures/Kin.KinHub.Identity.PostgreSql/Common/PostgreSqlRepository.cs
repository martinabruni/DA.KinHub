using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Identity.PostgreSql.Common;

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

    public async Task<TDomain> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        var entity = await Set.FindAsync([key], cancellationToken);
        if (entity is null)
            throw new EntityNotFoundException(typeof(TEntity).Name, key!);
        return entity.Adapt<TDomain>();
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
