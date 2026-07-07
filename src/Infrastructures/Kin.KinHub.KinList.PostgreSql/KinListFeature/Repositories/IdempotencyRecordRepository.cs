using Kin.KinHub.KinList.Domain.KinListFeature;
using Kin.KinHub.KinList.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.KinList.PostgreSql.KinListFeature;

public sealed class IdempotencyRecordRepository : IIdempotencyRecordRepository
{
    private readonly KinListDbContext _context;

    public IdempotencyRecordRepository(KinListDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyRecord?> GetActiveAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Key == key && x.FamilyId == familyId && x.UserId == userId && x.ExpiresAt > utcNow,
                cancellationToken);

        return entity is null
            ? null
            : new IdempotencyRecord
            {
                Id = entity.Id,
                Key = entity.Key,
                FamilyId = entity.FamilyId,
                UserId = entity.UserId,
                RequestHash = entity.RequestHash,
                ResponseJson = entity.ResponseJson,
                ExpiresAt = entity.ExpiresAt,
                CreatedAt = entity.CreatedAt,
            };
    }

    public async Task DeleteExpiredAsync(string key, Guid familyId, Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var expiredRecords = await _context.IdempotencyRecords
            .Where(x => x.Key == key && x.FamilyId == familyId && x.UserId == userId && x.ExpiresAt <= utcNow)
            .ToListAsync(cancellationToken);

        if (expiredRecords.Count is 0)
        {
            return;
        }

        _context.IdempotencyRecords.RemoveRange(expiredRecords);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var expiredRecords = await _context.IdempotencyRecords
            .Where(x => x.ExpiresAt <= utcNow)
            .ToListAsync(cancellationToken);

        if (expiredRecords.Count is 0)
        {
            return 0;
        }

        _context.IdempotencyRecords.RemoveRange(expiredRecords);
        await _context.SaveChangesAsync(cancellationToken);
        return expiredRecords.Count;
    }

    public async Task<IdempotencyRecord> AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        var entity = new IdempotencyRecordEntity
        {
            Id = record.Id,
            Key = record.Key,
            FamilyId = record.FamilyId,
            UserId = record.UserId,
            RequestHash = record.RequestHash,
            ResponseJson = record.ResponseJson,
            ExpiresAt = record.ExpiresAt,
            CreatedAt = record.CreatedAt,
        };

        _context.IdempotencyRecords.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }
}
