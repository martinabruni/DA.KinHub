namespace Kin.KinHub.Identity.Domain.Common;

public interface IRepository<TModel, TKey>
where TModel : class
{
    Task<TModel> CreateAsync(TModel model, CancellationToken cancellationToken = default);
    Task<TModel> DeleteAsync(TKey key, CancellationToken cancellationToken = default);
    Task<TModel> UpdateAsync(TKey key, TModel model, CancellationToken cancellationToken = default);
    Task<TModel> GetAsync(TKey key, CancellationToken cancellationToken = default);
}
