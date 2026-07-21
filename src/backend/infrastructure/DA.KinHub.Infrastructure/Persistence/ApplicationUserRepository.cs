using System.Data.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class ApplicationUserRepository(KinHubDbContext dbContext) : IApplicationUserRepository
{
    public async Task<ApplicationUser?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.ApplicationUsers
                .SingleOrDefaultAsync(
                    applicationUser => applicationUser.ExternalIssuer == externalIdentity.Issuer
                        && applicationUser.ExternalObjectId == externalIdentity.ObjectId,
                    cancellationToken);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The application user could not be loaded.", exception);
        }
    }

    public async Task<ApplicationUser> GetOrCreateAsync(ExternalIdentity externalIdentity, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        try
        {
            var applicationUser = ApplicationUser.Create(externalIdentity, createdAt);
            return await dbContext.ApplicationUsers
                .FromSqlInterpolated($"""
                    INSERT INTO shared.application_users (id, external_issuer, external_object_id, created_at, inactive_at)
                    VALUES ({applicationUser.Id}, {externalIdentity.Issuer}, {externalIdentity.ObjectId}, {createdAt}, {null})
                    ON CONFLICT (external_issuer, external_object_id)
                    DO UPDATE SET external_issuer = EXCLUDED.external_issuer
                    RETURNING id, external_issuer, external_object_id, created_at, inactive_at
                    """)
                .SingleAsync(cancellationToken);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The application user could not be stored.", exception);
        }
    }

    private static bool IsRepositoryUnavailable(Exception exception) =>
        exception is TimeoutException
        or DbException
        or DbUpdateException { InnerException: DbException };
}
