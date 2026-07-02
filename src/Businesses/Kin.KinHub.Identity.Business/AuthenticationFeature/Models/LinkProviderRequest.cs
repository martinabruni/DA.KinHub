namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public sealed class LinkProviderRequest
{
    public IdentityProviderType Provider { get; init; }

    public string? Password { get; init; }

    public string? ExternalToken { get; init; }
}
