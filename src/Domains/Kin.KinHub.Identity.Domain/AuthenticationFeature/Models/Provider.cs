using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;
namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

public sealed class Provider : BaseActivableEntity<int>
{
    public string? Name { get; set; }
    public string? Label { get; set; }
}
