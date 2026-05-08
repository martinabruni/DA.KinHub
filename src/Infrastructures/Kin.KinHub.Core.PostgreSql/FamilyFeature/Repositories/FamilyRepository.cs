using Kin.KinHub.Core.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Core.PostgreSql.FamilyFeature;

public sealed class FamilyRepository : PostgreSqlRepository<FamilyEntity, Family, Guid>, IFamilyRepository
{
    public FamilyRepository(CoreDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<Family?> FindByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(f => f.UserId == userId && !f.IsDeleted, cancellationToken);
        return entity?.Adapt<Family>();
    }
}
