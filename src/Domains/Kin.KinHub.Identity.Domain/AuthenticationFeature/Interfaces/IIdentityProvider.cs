using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

/// <summary>
/// Abstracts a single identity provider (KinHub password, Google, GitHub, Entra, ...).
/// Adding a new provider means implementing this interface and registering it; no
/// consumer (Core / KinRecipe / KinList) needs to change.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// The provider this adapter handles.
    /// </summary>
    IdentityProviderType ProviderType { get; }

    /// <summary>
    /// Authenticates the supplied credentials and returns the matching user, or
    /// <see langword="null"/> when authentication fails.
    /// </summary>
    Task<KinUser?> AuthenticateAsync(
        IdentityCredential credential,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Provisions a brand new user for this provider.
    /// </summary>
    Task<KinUser> RegisterAsync(
        IdentityRegistration registration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links this provider to an already existing user. Implementations must never
    /// perform automatic linking based on a matching email address.
    /// </summary>
    Task LinkAsync(
        Guid userId,
        IdentityCredential credential,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the link between this provider and the given user.
    /// </summary>
    Task UnlinkAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
