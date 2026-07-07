namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public sealed record OAuthIdentitySession(
    string SessionId,
    string RefreshToken,
    string Email,
    string? DisplayName,
    DateTimeOffset ExpiresAtUtc);
