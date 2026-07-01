namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

/// <summary>
/// A provider-agnostic credential presented for authentication or linking.
/// For the KinHub password provider this is an email/password pair; for external
/// providers <see cref="ExternalToken"/> carries the provider's assertion.
/// </summary>
public sealed class IdentityCredential
{
    public string? Email { get; init; }

    public string? Password { get; init; }

    /// <summary>
    /// Provider-issued token or authorization code for external providers.
    /// </summary>
    public string? ExternalToken { get; init; }
}
