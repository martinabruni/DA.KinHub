namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public sealed record OAuthAuthorizationCodeTicket(
    string Code,
    string ClientId,
    string RedirectUri,
    string Scope,
    string CodeChallenge,
    string CodeChallengeMethod,
    LoginResponse LoginResponse,
    DateTimeOffset ExpiresAtUtc);
