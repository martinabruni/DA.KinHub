using DomainKinList = Kin.KinHub.KinList.Domain.KinListFeature.KinList;
using DomainKinListItem = Kin.KinHub.KinList.Domain.KinListFeature.KinListItem;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IKinListMapper
{
    KinListResponse MapSummary(DomainKinList list, IReadOnlyList<DomainKinListItem> items);
    KinListDetailResponse MapDetail(DomainKinList list, IReadOnlyList<DomainKinListItem> items);
    KinListItemResponse MapItem(DomainKinListItem item);
}
