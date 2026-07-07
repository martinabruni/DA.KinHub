using Kin.KinHub.KinList.Domain.KinListFeature;
using Kin.KinHub.KinList.PostgreSql;
using Microsoft.EntityFrameworkCore;
using DomainKinList = Kin.KinHub.KinList.Domain.KinListFeature.KinList;

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
