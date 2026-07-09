using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

public sealed class KinUser : BaseDeletableEntity<Guid>
{
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public bool IsEmailVerified { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
}
