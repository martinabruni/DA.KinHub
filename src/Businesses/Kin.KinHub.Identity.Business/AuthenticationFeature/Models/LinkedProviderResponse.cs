namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public sealed class LinkedProviderResponse
{
    public required IdentityProviderType Provider { get; init; }

    public required string ProviderName { get; init; }

    public required DateTime LinkedAt { get; init; }
}
