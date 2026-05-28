namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public sealed class UpdateUserEmailRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewEmail { get; init; }
}
