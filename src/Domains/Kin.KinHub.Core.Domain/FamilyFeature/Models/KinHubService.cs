using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.Core.Domain.FamilyFeature;

public sealed class KinHubService : BaseActivableEntity<int>
{
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
}
