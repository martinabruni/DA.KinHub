using Kin.KinHub.KinList.Domain.KinListFeature;
using Kin.KinHub.KinList.PostgreSql.Models;
using Microsoft.EntityFrameworkCore;
using DomainKinList = Kin.KinHub.KinList.Domain.KinListFeature.KinList;
using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.KinList.PostgreSql.KinListFeature;

public sealed class KinListRepository : IKinListRepository
{
    private readonly KinListDbContext _context;

    public KinListRepository(KinListDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DomainKinList>> GetAllByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default) =>
        await _context.Lists
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderByDescending(x => x.LastModifiedAt)
            .Select(entity => Map(entity))
            .ToListAsync(cancellationToken);

    public async Task<DomainKinList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Lists.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<DomainKinList> AddAsync(DomainKinList list, CancellationToken cancellationToken = default)
    {
        var entity = Map(list);
        _context.Lists.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<DomainKinList> UpdateAsync(DomainKinList list, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Lists.FirstOrDefaultAsync(x => x.Id == list.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Kin list '{list.Id}' was not found.");
        entity.Title = list.Title;
        entity.Version = list.Version;
        entity.IsDeleted = list.IsDeleted;
        entity.UpdatedAt = list.UpdatedAt;
        entity.LastModifiedAt = list.LastModifiedAt;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static DomainKinList Map(KinListEntity entity) =>
        new()
        {
            Id = entity.Id,
            FamilyId = entity.FamilyId,
            Title = entity.Title,
            Version = entity.Version,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            LastModifiedAt = entity.LastModifiedAt,
        };

    private static KinListEntity Map(DomainKinList list) =>
        new()
        {
            Id = list.Id,
            FamilyId = list.FamilyId,
            Title = list.Title,
            Version = list.Version,
            IsDeleted = list.IsDeleted,
            CreatedAt = list.CreatedAt,
            UpdatedAt = list.UpdatedAt,
            LastModifiedAt = list.LastModifiedAt,
        };
}

public sealed class KinListItemRepository : IKinListItemRepository
{
    private readonly KinListDbContext _context;

    public KinListItemRepository(KinListDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DomainKinListItem>> GetAllByListIdAsync(Guid listId, CancellationToken cancellationToken = default) =>
        await _context.Items
            .AsNoTracking()
            .Where(x => x.ListId == listId)
            .OrderBy(x => x.IsCompleted)
            .ThenByDescending(x => x.ActivationOrder)
            .Select(entity => Map(entity))
            .ToListAsync(cancellationToken);

    public async Task<DomainKinListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Items.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<DomainKinListItem> AddAsync(DomainKinListItem item, CancellationToken cancellationToken = default)
    {
        var entity = Map(item);
        _context.Items.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<DomainKinListItem> UpdateAsync(DomainKinListItem item, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Items.FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Kin list item '{item.Id}' was not found.");
        entity.Text = item.Text;
        entity.Version = item.Version;
        entity.IsCompleted = item.IsCompleted;
        entity.ActivationOrder = item.ActivationOrder;
        entity.IsDeleted = item.IsDeleted;
        entity.UpdatedAt = item.UpdatedAt;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<long> GetNextActivationOrderAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var currentMax = await _context.Items
            .Where(x => x.ListId == listId && !x.IsDeleted)
            .MaxAsync(x => (long?)x.ActivationOrder, cancellationToken);
        return (currentMax ?? 0) + 1;
    }

    private static DomainKinListItem Map(KinListItemEntity entity) =>
        new()
        {
            Id = entity.Id,
            ListId = entity.ListId,
            Text = entity.Text,
            Version = entity.Version,
            IsCompleted = entity.IsCompleted,
            ActivationOrder = entity.ActivationOrder,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };

    private static KinListItemEntity Map(DomainKinListItem item) =>
        new()
        {
            Id = item.Id,
            ListId = item.ListId,
            Text = item.Text,
            Version = item.Version,
            IsCompleted = item.IsCompleted,
            ActivationOrder = item.ActivationOrder,
            IsDeleted = item.IsDeleted,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
}

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
