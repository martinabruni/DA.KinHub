using System.Data.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyRepository(KinHubDbContext dbContext) : IFamilyRepository
{
    private const string SingleActiveMembershipConstraint = "IX_family_memberships_single_active_user";

    public async Task<FamilyCreationPersistenceResult> CreateWithCreatorAsync(
        Guid applicationUserId,
        Family family,
        FamilyMembership membership,
        CancellationToken cancellationToken)
    {
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            FamilyCreationPersistenceResult? result = null;
            await strategy.ExecuteAsync(async (ct) =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

                try
                {
                    await LockApplicationUserAsync(applicationUserId, ct);

                    var existingFamilyId = await FindActiveFamilyIdAsync(applicationUserId, ct);
                    if (existingFamilyId is Guid activeFamilyId)
                    {
                        await transaction.RollbackAsync(ct);
                        result = new FamilyCreationPersistenceResult.Existing(activeFamilyId, ReconciledConflict: false);
                        return;
                    }

                    dbContext.Families.Add(family);
                    dbContext.FamilyMemberships.Add(membership);
                    await dbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    result = new FamilyCreationPersistenceResult.Created(family.Id);
                    return;
                }
                catch (DbUpdateException exception) when (IsExpectedConcurrentConflict(exception))
                {
                    await transaction.RollbackAsync(ct);
                    dbContext.ChangeTracker.Clear();

                    var existingFamilyId = await FindActiveFamilyIdAsync(applicationUserId, ct);
                    if (existingFamilyId is Guid activeFamilyId)
                    {
                        result = new FamilyCreationPersistenceResult.Existing(activeFamilyId, ReconciledConflict: true);
                        return;
                    }

                    throw;
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            }, cancellationToken);
            return result ?? throw new InvalidOperationException("Family creation did not return a result.");
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The family could not be created.", exception);
        }
    }

    private Task LockApplicationUserAsync(Guid applicationUserId, CancellationToken cancellationToken)
    {
        return dbContext.ApplicationUsers
            .FromSqlInterpolated($"SELECT * FROM shared.application_users WHERE \"Id\" = {applicationUserId} FOR UPDATE")
            .SingleAsync(cancellationToken);
    }

    private Task<Guid?> FindActiveFamilyIdAsync(Guid applicationUserId, CancellationToken cancellationToken)
    {
        return (from membership in dbContext.FamilyMemberships
                join family in dbContext.Families on membership.FamilyId equals family.Id
                where membership.ApplicationUserId == applicationUserId
                    && membership.InactiveAt == null
                    && family.InactiveAt == null
                orderby membership.CreatedAt
                select (Guid?)membership.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static bool IsExpectedConcurrentConflict(DbUpdateException exception)
        => exception.InnerException is PostgresException postgresException
           && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
           && string.Equals(postgresException.ConstraintName, SingleActiveMembershipConstraint, StringComparison.Ordinal);

    private static bool IsRepositoryUnavailable(Exception exception) =>
        exception is TimeoutException
        or DbException
        or DbUpdateException { InnerException: DbException }
        or InvalidOperationException { InnerException: DbException };
}
