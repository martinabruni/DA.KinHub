namespace Kin.KinHub.Shared.Api.Common;

public sealed class OAuthRegisteredClientOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string[] RedirectUris { get; set; } = [];
    public string[] GrantTypes { get; set; } = ["authorization_code"];
    public string[] ResponseTypes { get; set; } = ["code"];
    public string TokenEndpointAuthMethod { get; set; } = "none";
    public string Scope { get; set; } = OAuthScopes.Read;
}
