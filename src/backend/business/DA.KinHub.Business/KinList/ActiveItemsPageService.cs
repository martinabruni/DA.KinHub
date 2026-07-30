using System.Globalization;
using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.KinList;
using DA.KinHub.Domain.KinServices;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Business.KinList;

public sealed class ActiveItemsPageService(
    IKinServiceRepository kinServiceRepository,
    IActiveKinListItemRepository activeKinListItemRepository,
    IActiveItemsCursorCodec activeItemsCursorCodec,
    IOptions<PaginationReadOptions> paginationOptions) : IActiveItemsPageService
{
    private const string KinListServiceKey = "kinlist";

    public async Task<ActiveItemsPageDto> GetActiveItemsPageAsync(
        Guid applicationUserId,
        Guid familyId,
        int requestedPageSize,
        string? opaqueCursor,
        CancellationToken cancellationToken)
    {
        if (applicationUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Application user ID is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new InvalidOperationException("Family ID is required.");
        }

        if (requestedPageSize <= 0)
        {
            throw new BusinessValidationException(BusinessErrorCodes.PaginationPageSizeInvalid, "The pageSize query parameter must be a positive integer.");
        }

        try
        {
            var serviceAvailable = await kinServiceRepository.IsServiceAvailableAsync(familyId, KinListServiceKey, cancellationToken);
            if (!serviceAvailable)
            {
                throw new BusinessAccessDeniedException(BusinessErrorCodes.ServiceAccessDenied, "Access is not allowed.");
            }

            var maxPageSize = paginationOptions.Value.ReadMax;
            var effectivePageSize = Math.Min(requestedPageSize, maxPageSize);
            var direction = ActiveItemsCursorDirection.Next;
            ActiveItemsPageAnchor? anchor = null;

            if (!string.IsNullOrWhiteSpace(opaqueCursor))
            {
                var decoded = activeItemsCursorCodec.Decode(opaqueCursor, familyId, applicationUserId);
                direction = decoded.Direction;
                if (decoded.EffectivePageSize != effectivePageSize)
                {
                    throw new ActiveItemsCursorInvalidException("The cursor page size does not match the requested page size.");
                }

                anchor = decoded.Anchor;
            }

            var page = await activeKinListItemRepository.GetActiveItemsPageAsync(
                familyId,
                applicationUserId,
                direction,
                anchor,
                effectivePageSize,
                cancellationToken);

            if (anchor is not null && page.Items.Count == 0)
            {
                throw new ActiveItemsCursorInvalidException("The cursor is stale.");
            }

            var items = page.Items
                .Select(item => new ActiveItemsPageItemDto(
                    item.Id,
                    item.Name,
                    item.Categories.Select(category => new ActiveItemsPageCategoryDto(category.Id, category.Name)).ToArray(),
                    item.RemainingCategoryCount,
                    new ActiveItemsPageAuthorDto(DisplayName: null),
                    item.Revision.ToString(CultureInfo.InvariantCulture)))
                .ToArray();

            string? previousCursor = null;
            string? nextCursor = null;
            if (page.Items.Count > 0)
            {
                var firstAnchor = page.Items[0].Anchor;
                var lastAnchor = page.Items[^1].Anchor;

                if (anchor is not null)
                {
                    if (direction == ActiveItemsCursorDirection.Next)
                    {
                        previousCursor = activeItemsCursorCodec.Encode(familyId, applicationUserId, ActiveItemsCursorDirection.Previous, effectivePageSize, firstAnchor);
                    }
                    else
                    {
                        nextCursor = activeItemsCursorCodec.Encode(familyId, applicationUserId, ActiveItemsCursorDirection.Next, effectivePageSize, lastAnchor);
                    }
                }

                if (page.HasMore)
                {
                    if (direction == ActiveItemsCursorDirection.Next)
                    {
                        nextCursor = activeItemsCursorCodec.Encode(familyId, applicationUserId, ActiveItemsCursorDirection.Next, effectivePageSize, lastAnchor);
                    }
                    else
                    {
                        previousCursor = activeItemsCursorCodec.Encode(familyId, applicationUserId, ActiveItemsCursorDirection.Previous, effectivePageSize, firstAnchor);
                    }
                }
            }

            return new ActiveItemsPageDto(items, effectivePageSize, maxPageSize, previousCursor, nextCursor);
        }
        catch (BusinessAccessDeniedException)
        {
            throw;
        }
        catch (ActiveItemsCursorInvalidException exception)
        {
            throw new BusinessValidationException(BusinessErrorCodes.PaginationCursorInvalid, exception.Message);
        }
        catch (ProtectedDataUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.StorageUnavailable, "The active items cursor store is unavailable.", exception);
        }
        catch (RepositoryUnavailableException exception)
        {
            throw new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "The active items page could not be loaded.", exception);
        }
    }
}
