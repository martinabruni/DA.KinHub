using Kin.KinHub.Core.PostgreSql;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.FamilyFeature;

public sealed class FamilyServiceRepository : PostgreSqlRepository<FamilyServiceEntity, FamilyService, Guid>, IFamilyServiceRepository
{
    public FamilyServiceRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FamilyService>> GetByFamilyIdAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(s => s.FamilyId == familyId)
            .ToListAsync(cancellationToken);
        return entities.Adapt<IReadOnlyList<FamilyService>>();
    }

    /// <inheritdoc/>
    public async Task<FamilyService?> FindByFamilyAndServiceAsync(
        Guid familyId,
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(s => s.FamilyId == familyId && s.ServiceId == serviceId, cancellationToken);
        return entity?.Adapt<FamilyService>();
    }
}
