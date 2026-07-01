namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

/// <summary>
/// Provider-agnostic data needed to provision a new user.
/// </summary>
public sealed class IdentityRegistration
{
    public required string Email { get; init; }

    public string? DisplayName { get; init; }

    public string? Password { get; init; }

    public string? ExternalToken { get; init; }
}
