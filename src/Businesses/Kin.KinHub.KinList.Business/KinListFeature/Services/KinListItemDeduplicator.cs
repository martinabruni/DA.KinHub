using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListItemDeduplicator : IKinListItemDeduplicator
{
    public KinListDeduplicationResult Deduplicate(
        IReadOnlyList<string> candidateItems,
        IReadOnlyList<DomainKinListItem> existingItems)
    {
        var normalizedExistingItems = existingItems
            .Where(x => !x.IsDeleted)
            .GroupBy(x => NormalizeText(x.Text), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var proposals = new List<KinListItemDraftProposalResponse>(candidateItems.Count);
        var duplicates = new List<KinListExistingDuplicateResponse>();
        foreach (var text in candidateItems)
        {
            var normalizedText = NormalizeText(text);
            if (normalizedExistingItems.TryGetValue(normalizedText, out var duplicateItem))
            {
                proposals.Add(new KinListItemDraftProposalResponse
                {
                    Text = text,
                    IsSelectedByDefault = false,
                    DuplicateOfItemId = duplicateItem.Id,
                });

                duplicates.Add(new KinListExistingDuplicateResponse
                {
                    ItemId = duplicateItem.Id,
                    Text = duplicateItem.Text,
                    IsCompleted = duplicateItem.IsCompleted,
                });

                continue;
            }

            proposals.Add(new KinListItemDraftProposalResponse
            {
                Text = text,
                IsSelectedByDefault = true,
            });
        }

        return new KinListDeduplicationResult
        {
            Proposals = proposals,
            ExistingDuplicates = duplicates,
        };
    }

    private static string NormalizeText(string text) => text.Trim();
}
