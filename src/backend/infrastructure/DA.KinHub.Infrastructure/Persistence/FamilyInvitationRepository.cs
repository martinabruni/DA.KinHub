using System.Data;
using System.Data.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyInvitationRepository(KinHubDbContext dbContext) : IFamilyInvitationRepository
{
    public async Task<FamilyInvitationCreateResult> CreateAsync(FamilyInvitation invitation, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var activeCount = await (from current in dbContext.FamilyInvitations
                                     join family in dbContext.Families on current.FamilyId equals family.Id
                                     where current.FamilyId == invitation.FamilyId
                                         && family.InactiveAt == null
                                         && current.RevokedAt == null
                                         && current.ConsumedAt == null
                                         && current.ExpiresAt > nowUtc
                                     select current.Id)
                .CountAsync(cancellationToken);

            if (activeCount >= 5)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FamilyInvitationCreateResult.LimitReached();
            }

            dbContext.FamilyInvitations.Add(invitation);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new FamilyInvitationCreateResult.Created();
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The family invitation could not be created.", exception);
        }
    }

    public async Task<bool> RevokeAsync(Guid familyId, Guid invitationId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await dbContext.FamilyInvitations
                .Where(invitation => invitation.FamilyId == familyId
                    && invitation.Id == invitationId
                    && invitation.RevokedAt == null
                    && invitation.ConsumedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(invitation => invitation.RevokedAt, revokedAt), cancellationToken);

            return updated == 1;
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The family invitation could not be revoked.", exception);
        }
    }

    public async Task<FamilyInvitationConsumeResult> ConsumeAsync(Guid applicationUserId, IReadOnlyList<InvitationCodeHmacCandidate> candidates, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var invitation = await FindMatchingInvitationAsync(candidates, nowUtc, cancellationToken);
            if (invitation is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FamilyInvitationConsumeResult.InvalidCode();
            }

            var activeFamilyId = await (from membership in dbContext.FamilyMemberships
                                        join family in dbContext.Families on membership.FamilyId equals family.Id
                                        where membership.ApplicationUserId == applicationUserId
                                            && membership.InactiveAt == null
                                            && family.InactiveAt == null
                                        select (Guid?)membership.FamilyId)
                .SingleOrDefaultAsync(cancellationToken);

            if (activeFamilyId is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FamilyInvitationConsumeResult.AlreadyMember(activeFamilyId.Value);
            }

            var consumeUpdated = await dbContext.FamilyInvitations
                .Where(current => current.Id == invitation.Id
                    && current.RevokedAt == null
                    && current.ConsumedAt == null
                    && current.ExpiresAt > nowUtc)
                .ExecuteUpdateAsync(setters => setters.SetProperty(current => current.ConsumedAt, nowUtc), cancellationToken);

            if (consumeUpdated != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FamilyInvitationConsumeResult.InvalidCode();
            }

            var reactivated = await ReactivateMembershipAsync(applicationUserId, invitation.FamilyId, cancellationToken);
            if (!reactivated)
            {
                dbContext.FamilyMemberships.Add(FamilyMembership.Create(applicationUserId, invitation.FamilyId, nowUtc));
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new FamilyInvitationConsumeResult.Consumed(invitation.FamilyId);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The family invitation could not be consumed.", exception);
        }
    }

    private async Task<FamilyInvitation?> FindMatchingInvitationAsync(IReadOnlyList<InvitationCodeHmacCandidate> candidates, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var invitations = await dbContext.FamilyInvitations
            .Where(invitation => invitation.RevokedAt == null && invitation.ConsumedAt == null && invitation.ExpiresAt > nowUtc)
            .ToListAsync(cancellationToken);

        return invitations.FirstOrDefault(invitation => candidates.Any(candidate => invitation.HmacKeyVersion == candidate.KeyVersion && invitation.CodeHmac.SequenceEqual(candidate.CodeHmac)));
    }

    private async Task<bool> ReactivateMembershipAsync(Guid applicationUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var membershipId = await dbContext.FamilyMemberships
            .Where(membership => membership.ApplicationUserId == applicationUserId
                && membership.FamilyId == familyId
                && membership.InactiveAt != null)
            .OrderByDescending(membership => membership.CreatedAt)
            .Select(membership => (Guid?)membership.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (membershipId is null)
        {
            return false;
        }

        var updated = await dbContext.FamilyMemberships
            .Where(membership => membership.Id == membershipId.Value && membership.InactiveAt != null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(membership => membership.InactiveAt, (DateTimeOffset?)null), cancellationToken);

        return updated == 1;
    }

    private static bool IsRepositoryUnavailable(Exception exception)
        => exception is TimeoutException
        or DbException
        or DbUpdateException { InnerException: DbException };
}
