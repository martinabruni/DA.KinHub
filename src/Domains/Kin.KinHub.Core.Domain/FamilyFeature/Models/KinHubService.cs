using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.FamilyFeature;

public sealed class KinHubService : BaseActivableEntity<int>
{
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
}
