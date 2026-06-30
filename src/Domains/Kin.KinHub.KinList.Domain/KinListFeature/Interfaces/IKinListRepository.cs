namespace Kin.KinHub.KinList.Domain.KinListFeature;

public interface IKinListRepository
{
    Task<IReadOnlyList<KinList>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);
    Task<KinList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KinList> AddAsync(KinList list, CancellationToken cancellationToken = default);
    Task<KinList> UpdateAsync(KinList list, CancellationToken cancellationToken = default);
}
