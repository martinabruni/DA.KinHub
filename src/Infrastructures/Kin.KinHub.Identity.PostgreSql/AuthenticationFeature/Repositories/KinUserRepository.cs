using Kin.KinHub.Identity.PostgreSql;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Identity.PostgreSql.AuthenticationFeature;

public sealed class KinUserRepository : PostgreSqlRepository<KinUserEntity, KinUser, Guid>, IKinUserRepository
{
    public KinUserRepository(IdentityDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<KinUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        return entity?.Adapt<KinUser>();
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeCreateAsync(KinUserEntity entity, CancellationToken cancellationToken)
    {
        var duplicate = await Set
            .AnyAsync(u => u.Email.ToLower() == entity.Email.ToLower(), cancellationToken);

        if (duplicate)
            throw new DuplicateEntityException(nameof(KinUser), nameof(KinUserEntity.Email), entity.Email);
    }
}
