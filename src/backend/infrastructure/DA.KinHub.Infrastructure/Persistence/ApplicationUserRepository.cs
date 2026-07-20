using DA.KinHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class ApplicationUserRepository(KinHubDbContext dbContext) : IApplicationUserRepository
{
    public Task<ApplicationUser?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken) =>
        dbContext.ApplicationUsers
            .SingleOrDefaultAsync(
                applicationUser => applicationUser.ExternalIssuer == externalIdentity.Issuer
                    && applicationUser.ExternalObjectId == externalIdentity.ObjectId,
                cancellationToken);

    public async Task<ApplicationUser> GetOrCreateAsync(ExternalIdentity externalIdentity, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        var existing = await FindByExternalIdentityAsync(externalIdentity, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var applicationUser = ApplicationUser.Create(externalIdentity, createdAt);
        dbContext.ApplicationUsers.Add(applicationUser);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return applicationUser;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.Entry(applicationUser).State = EntityState.Detached;
            var resolved = await FindByExternalIdentityAsync(externalIdentity, cancellationToken);
            if (resolved is null)
            {
                throw;
            }

            return resolved;
        }
    }
}
