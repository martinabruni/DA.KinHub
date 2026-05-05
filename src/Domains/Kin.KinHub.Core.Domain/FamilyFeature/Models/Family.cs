using Kin.KinHub.Core.Domain.Common;

namespace Kin.KinHub.Core.Domain.FamilyFeature;

public sealed class Family : BaseDeletableEntity<Guid>
{
    public required string Name { get; set; }
    public required Guid UserId { get; set; }
}
