using DA.KinHub.Business.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using DA.KinHub.Domain.Identity;

namespace DA.KinHub.Business.Identity;

public sealed class FamilyInvitationService(
    IApplicationUserRepository applicationUserRepository,
    IFamilyInvitationRepository familyInvitationRepository,
    IFamilyInvitationCodeProtector codeProtector,
    TimeProvider timeProvider) : IFamilyInvitationService
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public async Task<CreatedFamilyInvitationDto> CreateAsync(Guid familyId, Guid applicationUserId, CancellationToken cancellationToken)
    {
        ValidateIdentifiers(familyId, applicationUserId);

        try
        {
            var nowUtc = timeProvider.GetUtcNow();
            var code = codeProtector.CreateNewCode();
            var invitation = FamilyInvitation.CreateStored(
                Guid.NewGuid(),
                familyId,
                applicationUserId,
                nowUtc,
                nowUtc.Add(InvitationLifetime),
                code.Candidate.CodeHmac,
                code.Candidate.KeyVersion);

            var result = await familyInvitationRepository.CreateAsync(invitation, nowUtc, cancellationToken);
            if (result is FamilyInvitationCreateResult.LimitReached)
            {
                throw new BusinessConflictException(BusinessErrorCodes.FamilyInvitationLimitReached, "The family already has the maximum number of active invitations.");
            }

            return new CreatedFamilyInvitationDto(invitation.Id, code.DisplayCode, invitation.ExpiresAt);
        }
        catch (BusinessConflictException)
        {
            throw;
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family invitation could not be created.", exception);
        }
    }

    public async Task RevokeAsync(Guid familyId, Guid invitationId, CancellationToken cancellationToken)
    {
        if (familyId == Guid.Empty || invitationId == Guid.Empty)
        {
            throw new InvalidOperationException("Family ID and invitation ID are required.");
        }

        try
        {
            var revoked = await familyInvitationRepository.RevokeAsync(familyId, invitationId, timeProvider.GetUtcNow(), cancellationToken);
            if (!revoked)
            {
                throw new BusinessConflictException(BusinessErrorCodes.FamilyInvitationNotFound, "The family invitation could not be found.");
            }
        }
        catch (BusinessConflictException)
        {
            throw;
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family invitation could not be revoked.", exception);
        }
    }

    public async Task<JoinFamilyInvitationResultDto> JoinAsync(ExternalIdentity externalIdentity, string? code, CancellationToken cancellationToken)
    {
        try
        {
            var nowUtc = timeProvider.GetUtcNow();
            var applicationUser = await applicationUserRepository.GetOrCreateAsync(externalIdentity, nowUtc, cancellationToken);
            if (!applicationUser.IsActive)
            {
                throw new BusinessAccessDeniedException("auth.profileInactive", "The signed-in profile is inactive.");
            }

            var normalizedCode = NormalizeCode(code);
            var candidates = codeProtector.CreateLookupCandidates(normalizedCode);
            var result = await familyInvitationRepository.ConsumeAsync(applicationUser.Id, candidates, nowUtc, cancellationToken);

            return result switch
            {
                FamilyInvitationConsumeResult.Consumed consumed => new JoinFamilyInvitationResultDto(consumed.FamilyId),
                FamilyInvitationConsumeResult.AlreadyMember alreadyMember => throw new BusinessConflictException(BusinessErrorCodes.FamilyMembershipAlreadyActive, $"The signed-in user already belongs to family '{alreadyMember.FamilyId}'."),
                FamilyInvitationConsumeResult.InvalidCode => throw new BusinessConflictException(BusinessErrorCodes.FamilyInvitationInvalid, "The invitation code is invalid or no longer available."),
                _ => throw new InvalidOperationException("Unexpected family invitation join result.")
            };
        }
        catch (BusinessValidationException)
        {
            throw;
        }
        catch (BusinessConflictException)
        {
            throw;
        }
        catch (BusinessAccessDeniedException)
        {
            throw;
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family invitation could not be consumed.", exception);
        }
        catch (DomainException exception)
        {
            throw new BusinessValidationException(BusinessErrorCodes.FamilyInvitationInvalid, exception.Message);
        }
    }

    private string NormalizeCode(string? code)
    {
        try
        {
            return codeProtector.Normalize(code);
        }
        catch (DomainException exception)
        {
            throw new BusinessValidationException(BusinessErrorCodes.FamilyInvitationInvalid, exception.Message);
        }
    }

    private static void ValidateIdentifiers(Guid familyId, Guid applicationUserId)
    {
        if (familyId == Guid.Empty || applicationUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Family ID and application user ID are required.");
        }
    }
}
