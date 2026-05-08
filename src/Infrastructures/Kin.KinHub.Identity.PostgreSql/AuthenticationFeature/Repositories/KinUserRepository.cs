using Kin.KinHub.Identity.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Identity.PostgreSql.AuthenticationFeature;

public sealed class KinUserRepository : PostgreSqlRepository<KinUserEntity, KinUser, Guid>, IKinUserRepository
{
    public KinUserRepository(IdentityDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<KinUser?> FindByEmailAsync(string email)
    {
        var entity = await Set
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        return entity?.Adapt<KinUser>();
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeCreateAsync(KinUserEntity entity)
    {
        var duplicate = await Set
            .AnyAsync(u => u.Email.ToLower() == entity.Email.ToLower());

        if (duplicate)
            throw new DuplicateEntityException(nameof(KinUser), nameof(KinUserEntity.Email), entity.Email);
    }
}
