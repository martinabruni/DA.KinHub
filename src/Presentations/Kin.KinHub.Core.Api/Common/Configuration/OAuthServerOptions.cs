namespace Kin.KinHub.Core.Api.Common;

public sealed class OAuthServerOptions
{
    public const string RateLimitPolicyName = "KinHubOAuthPolicy";
    public const string SectionName = "OAuth";
    public string AuthorizationServerUrl { get; set; } = "https://localhost";
    public string DocumentationUrl { get; set; } = "https://github.com/martinabruni/Kin.KinHub";
    public string RegistrationUiUrl { get; set; } = string.Empty;
    public string[] SupportedScopes { get; set; } = [OAuthScopes.Read, OAuthScopes.Write, OAuthScopes.Admin];
    public string[] DynamicClientDefaultScopes { get; set; } = [OAuthScopes.Read];
    public string[] DynamicClientAllowedScopes { get; set; } = [OAuthScopes.Read, OAuthScopes.Write];
    public string[] ElevatedConsentScopes { get; set; } = [OAuthScopes.Write, OAuthScopes.Admin];
    public OAuthRegisteredClientOptions[] Clients { get; set; } = [];
    public int AuthorizationCodeLifetimeMinutes { get; set; } = 5;
    public int SessionLifetimeHours { get; set; } = 12;
    public string SessionCookieName { get; set; } = "kinhub_identity_session";
    public bool EnableDynamicClientRegistration { get; set; }
    public int MaxRegisteredClients { get; set; } = 100;
    public int MaxAuthorizationCodes { get; set; } = 1000;
    public int MaxScopedRefreshTokens { get; set; } = 1000;
    public int MaxIdentitySessions { get; set; } = 1000;
    public int RateLimitPermitLimit { get; set; } = 30;
    public int RateLimitWindowSeconds { get; set; } = 60;
}
