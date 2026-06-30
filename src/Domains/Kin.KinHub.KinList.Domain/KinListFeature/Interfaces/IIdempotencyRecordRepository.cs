namespace Kin.KinHub.KinList.Domain.KinListFeature;

public interface IIdempotencyRecordRepository
{
    Task<IdempotencyRecord?> GetActiveAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task DeleteExpiredAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IdempotencyRecord> AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
}
