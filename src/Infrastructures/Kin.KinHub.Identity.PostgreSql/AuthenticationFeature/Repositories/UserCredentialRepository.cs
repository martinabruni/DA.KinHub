using Kin.KinHub.Identity.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Identity.PostgreSql.AuthenticationFeature;

public sealed class UserCredentialRepository
    : PostgreSqlRepository<UserCredentialEntity, UserCredential, Guid>, IUserCredentialRepository
{
    public UserCredentialRepository(IdentityDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<UserCredential?> GetByUserIdAsync(Guid userId)
    {
        var entity = await Set.FirstOrDefaultAsync(x => x.UserId == userId);
        return entity?.Adapt<UserCredential>();
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeCreateAsync(UserCredentialEntity entity)
    {
        var duplicate = await Set.AnyAsync(x => x.UserId == entity.UserId);

        if (duplicate)
            throw new DuplicateEntityException(
                nameof(UserCredential),
                nameof(UserCredentialEntity.UserId),
                entity.UserId);
    }
}
