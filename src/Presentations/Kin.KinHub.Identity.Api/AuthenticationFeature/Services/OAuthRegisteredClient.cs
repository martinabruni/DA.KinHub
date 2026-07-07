namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public sealed record OAuthRegisteredClient(
    string ClientId,
    string ClientName,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> GrantTypes,
    IReadOnlyList<string> ResponseTypes,
    string TokenEndpointAuthMethod,
    string Scope,
    DateTimeOffset ClientIdIssuedAt);
