using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IKinListItemDeduplicator
{
    KinListDeduplicationResult Deduplicate(
        IReadOnlyList<string> candidateItems,
        IReadOnlyList<DomainKinListItem> existingItems);
}
