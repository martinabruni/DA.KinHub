namespace DA.KinHub.Business.KinList;

public sealed record ActiveItemsPageCategoryDto(Guid Id, string Name);

public sealed record ActiveItemsPageAuthorDto(string? DisplayName);

public sealed record ActiveItemsPageItemDto(
    Guid Id,
    string Name,
    IReadOnlyList<ActiveItemsPageCategoryDto> Categories,
    int RemainingCategoryCount,
    ActiveItemsPageAuthorDto Author,
    string Version);

public sealed record ActiveItemsPageDto(
    IReadOnlyList<ActiveItemsPageItemDto> Items,
    int EffectivePageSize,
    int MaxPageSize,
    string? PreviousCursor,
    string? NextCursor);

public interface IActiveItemsPageService
{
    Task<ActiveItemsPageDto> GetActiveItemsPageAsync(
        Guid applicationUserId,
        Guid familyId,
        int requestedPageSize,
        string? opaqueCursor,
        CancellationToken cancellationToken);
}
