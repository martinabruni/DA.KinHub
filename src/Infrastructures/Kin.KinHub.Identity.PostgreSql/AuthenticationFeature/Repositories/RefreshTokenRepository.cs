using Kin.KinHub.Identity.PostgreSql.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.Identity.PostgreSql.AuthenticationFeature;

public sealed class RefreshTokenRepository : PostgreSqlRepository<RefreshTokenEntity, RefreshToken, Guid>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IdentityDbContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<RefreshToken?> FindByTokenAsync(string token)
    {
        var entity = await Set
            .FirstOrDefaultAsync(x => x.Token == token);
        return entity?.Adapt<RefreshToken>();
    }

    /// <inheritdoc/>
    public async Task RevokeAllByUserIdAsync(Guid userId)
    {
        var tokens = await Set
            .Where(x => x.UserId == userId && !x.Revoked)
            .ToListAsync();

        if (tokens.Count == 0)
            return;

        foreach (var token in tokens)
            token.Revoked = true;

        await Context.SaveChangesAsync();
    }
}
