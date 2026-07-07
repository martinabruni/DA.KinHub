using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public sealed class OAuthAuthorizeLoginRequest
{
    [FromForm(Name = "response_type")]
    public string? ResponseType { get; set; }

    [FromForm(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromForm(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }

    [FromForm(Name = "state")]
    public string? State { get; set; }

    [FromForm(Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    [FromForm(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }

    [FromForm(Name = "email")]
    public string? Email { get; set; }

    [FromForm(Name = "password")]
    public string? Password { get; set; }

    [FromForm(Name = "decision")]
    public string? Decision { get; set; }

    [FromForm(Name = "approve_elevated_access")]
    public bool ApproveElevatedAccess { get; set; }
}
