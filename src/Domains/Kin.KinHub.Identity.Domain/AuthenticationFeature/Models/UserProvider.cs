using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;
namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

public sealed class UserProvider : BaseDeletableEntity<Guid>
{
    public required Guid UserId { get; set; }
    public required int ProviderId { get; set; }
    public required string ProviderUserId { get; set; }
}
