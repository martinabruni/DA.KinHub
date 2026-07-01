namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

/// <summary>
/// Resolves registered <see cref="IIdentityProvider"/> adapters by their provider type.
/// New providers become available simply by registering another <see cref="IIdentityProvider"/>.
/// </summary>
public sealed class IdentityProviderRegistry : IIdentityProviderRegistry
{
    private readonly IReadOnlyDictionary<IdentityProviderType, IIdentityProvider> _providers;

    public IdentityProviderRegistry(IEnumerable<IIdentityProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.ProviderType);
    }

    public IIdentityProvider? Resolve(IdentityProviderType providerType) =>
        _providers.TryGetValue(providerType, out var provider) ? provider : null;
}
