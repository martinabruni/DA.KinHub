using Kin.KinHub.Identity.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Identity.PostgreSql.AuthenticationFeature;

public sealed class UserProviderRepository
    : PostgreSqlRepository<UserProviderEntity, UserProvider, Guid>, IUserProviderRepository
{
    public UserProviderRepository(IdentityDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserProvider>> GetByUserIdAsync(Guid userId)
    {
        var entities = await Set
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .ToListAsync();

        return entities.Select(x => x.Adapt<UserProvider>()).ToList();
    }

    /// <inheritdoc/>
    public async Task<UserProvider?> GetByUserAndProviderAsync(Guid userId, int providerId)
    {
        var entity = await Set
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ProviderId == providerId && !x.IsDeleted);

        return entity?.Adapt<UserProvider>();
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeCreateAsync(UserProviderEntity entity)
    {
        var duplicate = await Set
            .AnyAsync(x => x.UserId == entity.UserId && x.ProviderId == entity.ProviderId && !x.IsDeleted);

        if (duplicate)
            throw new DuplicateEntityException(
                nameof(UserProvider),
                $"{nameof(UserProviderEntity.UserId)}/{nameof(UserProviderEntity.ProviderId)}",
                $"{entity.UserId}/{entity.ProviderId}");
    }
}
