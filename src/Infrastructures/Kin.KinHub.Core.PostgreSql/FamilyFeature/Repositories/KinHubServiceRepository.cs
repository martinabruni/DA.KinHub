using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.FamilyFeature;

public sealed class KinHubServiceRepository : PostgreSqlRepository<KinHubServiceEntity, KinHubService, int>, IKinHubServiceRepository
{
    public KinHubServiceRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<KinHubService?> FindByServiceTypeAsync(
        KinHubServiceType serviceType,
        CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(s => s.Id == (int)serviceType, cancellationToken);
        return entity?.Adapt<KinHubService>();
    }
}
