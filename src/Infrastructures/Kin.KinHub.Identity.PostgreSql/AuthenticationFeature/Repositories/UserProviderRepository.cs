using Kin.KinHub.Identity.PostgreSql;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Identity.PostgreSql.AuthenticationFeature;

public sealed class UserProviderRepository
    : PostgreSqlRepository<UserProviderEntity, UserProvider, Guid>, IUserProviderRepository
{
    public UserProviderRepository(IdentityDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserProvider>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entities = await Set
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.Adapt<UserProvider>()).ToList();
    }

    /// <inheritdoc/>
    public async Task<UserProvider?> GetByUserAndProviderAsync(Guid userId, int providerId, CancellationToken cancellationToken = default)
    {
        var entity = await Set
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ProviderId == providerId && !x.IsDeleted, cancellationToken);

        return entity?.Adapt<UserProvider>();
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeCreateAsync(UserProviderEntity entity, CancellationToken cancellationToken)
    {
        var duplicate = await Set
            .AnyAsync(x => x.UserId == entity.UserId && x.ProviderId == entity.ProviderId && !x.IsDeleted, cancellationToken);

        if (duplicate)
            throw new DuplicateEntityException(
                nameof(UserProvider),
                $"{nameof(UserProviderEntity.UserId)}/{nameof(UserProviderEntity.ProviderId)}",
                $"{entity.UserId}/{entity.ProviderId}");
    }
}
