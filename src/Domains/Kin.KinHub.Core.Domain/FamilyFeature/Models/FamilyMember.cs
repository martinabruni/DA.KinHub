using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.Core.Domain.FamilyFeature;

public sealed class FamilyMember : BaseDeletableEntity<Guid>
{
    public required string Name { get; set; }
    public required Guid FamilyId { get; set; }
}
