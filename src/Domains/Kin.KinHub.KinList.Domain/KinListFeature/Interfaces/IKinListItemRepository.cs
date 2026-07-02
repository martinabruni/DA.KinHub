namespace Kin.KinHub.KinList.Domain.KinListFeature;

public interface IKinListItemRepository
{
    Task<IReadOnlyList<KinListItem>> GetAllByListIdAsync(Guid listId, CancellationToken cancellationToken = default);
    Task<KinListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KinListItem> AddAsync(KinListItem item, CancellationToken cancellationToken = default);
    Task<KinListItem> UpdateAsync(KinListItem item, CancellationToken cancellationToken = default);
    Task<long> GetNextActivationOrderAsync(Guid listId, CancellationToken cancellationToken = default);
}
