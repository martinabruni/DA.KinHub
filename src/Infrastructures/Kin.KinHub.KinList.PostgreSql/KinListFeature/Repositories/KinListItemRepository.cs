using Kin.KinHub.KinList.Domain.KinListFeature;
using Kin.KinHub.KinList.PostgreSql;
using Microsoft.EntityFrameworkCore;
using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.KinList.PostgreSql.KinListFeature;

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
