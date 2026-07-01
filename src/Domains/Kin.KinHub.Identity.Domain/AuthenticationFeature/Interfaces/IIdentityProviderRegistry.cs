namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

/// <summary>
/// Resolves the <see cref="IIdentityProvider"/> adapter for a given provider type.
/// </summary>
public interface IIdentityProviderRegistry
{
    /// <summary>
    /// Returns the adapter for <paramref name="providerType"/>, or <see langword="null"/>
    /// when no adapter is registered for it.
    /// </summary>
    IIdentityProvider? Resolve(IdentityProviderType providerType);
}
