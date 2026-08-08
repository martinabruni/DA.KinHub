using DA.KinHub.Business.Common;
using DA.KinHub.Business.KinList;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Business.Identity;

public sealed class FamilySettingsService(
    IFamilyDetailsRepository familyDetailsRepository,
    IFamilyMemberPageRepository familyMemberPageRepository,
    IFamilyInvitationPageRepository familyInvitationPageRepository,
    IFamilyMemberCursorCodec familyMemberCursorCodec,
    IFamilyInvitationCursorCodec familyInvitationCursorCodec,
    IOptions<PaginationReadOptions> paginationOptions,
    TimeProvider timeProvider) : IFamilySettingsService
{
    public async Task<FamilyDetailsDto> GetFamilyDetailsAsync(Guid familyId, CancellationToken cancellationToken)
    {
        if (familyId == Guid.Empty)
        {
            throw new InvalidOperationException("Family ID is required.");
        }

        try
        {
            var details = await familyDetailsRepository.GetFamilyDetailsAsync(familyId, cancellationToken);
            if (details is null)
            {
                throw new BusinessAccessDeniedException(BusinessErrorCodes.FamilyAccessDenied, "Access is not allowed.");
            }

            return new FamilyDetailsDto(details.Name);
        }
        catch (BusinessAccessDeniedException)
        {
            throw;
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family details could not be loaded.", exception);
        }
    }

    public async Task<FamilyMembersPageDto> GetFamilyMembersPageAsync(Guid familyId, Guid applicationUserId, int requestedPageSize, string? opaqueCursor, CancellationToken cancellationToken)
    {
        ValidateFamilyId(familyId);
        if (applicationUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Application user ID is required.");
        }
        var effectivePageSize = ValidateAndClampPageSize(requestedPageSize);

        try
        {
            var direction = FamilyPageCursorDirection.Next;
            FamilyMemberPageAnchor? anchor = null;
            if (!string.IsNullOrWhiteSpace(opaqueCursor))
            {
                var decoded = familyMemberCursorCodec.Decode(opaqueCursor, familyId);
                direction = decoded.Direction;
                if (decoded.EffectivePageSize != effectivePageSize)
                {
                    throw new FamilyPageCursorInvalidException("The cursor page size does not match the requested page size.");
                }

                anchor = decoded.Anchor;
            }

            var page = await familyMemberPageRepository.GetFamilyMembersPageAsync(familyId, direction, anchor, effectivePageSize, cancellationToken);
            if (anchor is not null && page.Items.Count == 0)
            {
                throw new FamilyPageCursorInvalidException("The cursor is stale.");
            }

            if (anchor is null && page.Items.Count == 0)
            {
                throw new BusinessConflictException(BusinessErrorCodes.FamilyStateInconsistent, "The family state is inconsistent.");
            }

            var items = page.Items.Select(item => new FamilyMemberDto(item.DisplayName, item.Initials, item.ApplicationUserId == applicationUserId)).ToArray();
            BuildPageCursors(
                familyId,
                effectivePageSize,
                direction,
                anchor,
                page.Items,
                page.HasMore,
                item => item.Anchor,
                familyMemberCursorCodec.Encode,
                out var previousCursor,
                out var nextCursor);

            return new FamilyMembersPageDto(items, effectivePageSize, paginationOptions.Value.ReadMax, previousCursor, nextCursor);
        }
        catch (BusinessValidationException)
        {
            throw;
        }
        catch (BusinessConflictException)
        {
            throw;
        }
        catch (ProtectedDataUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.StorageUnavailable, "The family members cursor store is unavailable.", exception);
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family members page could not be loaded.", exception);
        }
        catch (FamilyPageCursorInvalidException exception)
        {
            throw new BusinessValidationException(BusinessErrorCodes.PaginationCursorInvalid, exception.Message);
        }
    }

    public async Task<FamilyInvitationsPageDto> GetActiveFamilyInvitationsPageAsync(Guid familyId, int requestedPageSize, string? opaqueCursor, CancellationToken cancellationToken)
    {
        ValidateFamilyId(familyId);
        var effectivePageSize = ValidateAndClampPageSize(requestedPageSize);
        var nowUtc = timeProvider.GetUtcNow();
        try
        {
            var direction = FamilyPageCursorDirection.Next;
            FamilyInvitationPageAnchor? anchor = null;
            if (!string.IsNullOrWhiteSpace(opaqueCursor))
            {
                var decoded = familyInvitationCursorCodec.Decode(opaqueCursor, familyId);
                direction = decoded.Direction;
                if (decoded.EffectivePageSize != effectivePageSize)
                {
                    throw new FamilyPageCursorInvalidException("The cursor page size does not match the requested page size.");
                }

                anchor = decoded.Anchor;
            }

            var page = await familyInvitationPageRepository.GetActiveFamilyInvitationsPageAsync(familyId, direction, anchor, effectivePageSize, nowUtc, cancellationToken);
            if (anchor is not null && page.Items.Count == 0)
            {
                throw new FamilyPageCursorInvalidException("The cursor is stale.");
            }

            var items = page.Items.Select(item => new FamilyInvitationDto(
                item.Id,
                new FamilyInvitationCreatorDto(item.Creator.DisplayName, item.Creator.Initials),
                item.CreatedAt,
                item.ExpiresAt,
                "active")).ToArray();

            BuildPageCursors(
                familyId,
                effectivePageSize,
                direction,
                anchor,
                page.Items,
                page.HasMore,
                item => item.Anchor,
                familyInvitationCursorCodec.Encode,
                out var previousCursor,
                out var nextCursor);

            return new FamilyInvitationsPageDto(items, effectivePageSize, paginationOptions.Value.ReadMax, previousCursor, nextCursor);
        }
        catch (ProtectedDataUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.StorageUnavailable, "The family invitations cursor store is unavailable.", exception);
        }
        catch (BusinessConflictException)
        {
            throw;
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The family invitations page could not be loaded.", exception);
        }
        catch (FamilyPageCursorInvalidException exception)
        {
            throw new BusinessValidationException(BusinessErrorCodes.PaginationCursorInvalid, exception.Message);
        }
    }

    private static void ValidateFamilyId(Guid familyId)
    {
        if (familyId == Guid.Empty)
        {
            throw new InvalidOperationException("Family ID is required.");
        }
    }

    private int ValidateAndClampPageSize(int requestedPageSize)
    {
        if (requestedPageSize <= 0)
        {
            throw new BusinessValidationException(BusinessErrorCodes.PaginationPageSizeInvalid, "The pageSize query parameter must be a positive integer.");
        }

        return Math.Min(requestedPageSize, paginationOptions.Value.ReadMax);
    }

    private static void BuildPageCursors<TItem, TAnchor>(
        Guid familyId,
        int effectivePageSize,
        FamilyPageCursorDirection direction,
        TAnchor? inputAnchor,
        IReadOnlyList<TItem> items,
        bool hasMore,
        Func<TItem, TAnchor> getAnchor,
        Func<Guid, FamilyPageCursorDirection, int, TAnchor, string> encodeCursor,
        out string? previousCursor,
        out string? nextCursor)
    {
        previousCursor = null;
        nextCursor = null;

        if (items.Count == 0)
        {
            return;
        }

        var firstAnchor = getAnchor(items[0]);
        var lastAnchor = getAnchor(items[^1]);

        if (inputAnchor is not null)
        {
            if (direction == FamilyPageCursorDirection.Next)
            {
                previousCursor = encodeCursor(familyId, FamilyPageCursorDirection.Previous, effectivePageSize, firstAnchor);
            }
            else
            {
                nextCursor = encodeCursor(familyId, FamilyPageCursorDirection.Next, effectivePageSize, lastAnchor);
            }
        }

        if (!hasMore)
        {
            return;
        }

        if (direction == FamilyPageCursorDirection.Next)
        {
            nextCursor = encodeCursor(familyId, FamilyPageCursorDirection.Next, effectivePageSize, lastAnchor);
        }
        else
        {
            previousCursor = encodeCursor(familyId, FamilyPageCursorDirection.Previous, effectivePageSize, firstAnchor);
        }
    }
}
