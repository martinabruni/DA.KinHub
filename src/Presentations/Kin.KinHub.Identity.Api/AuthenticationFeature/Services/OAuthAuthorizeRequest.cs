using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public sealed class OAuthAuthorizeRequest
{
    [FromQuery(Name = "response_type")]
    public string? ResponseType { get; set; }

    [FromQuery(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromQuery(Name = "scope")]
    public string? Scope { get; set; }

    [FromQuery(Name = "state")]
    public string? State { get; set; }

    [FromQuery(Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    [FromQuery(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }
}
