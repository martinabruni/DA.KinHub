using FluentValidation;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

internal sealed class OAuthDynamicClientRegistrationRequestValidator : AbstractValidator<OAuthDynamicClientRegistrationRequest>
{
    private static readonly string[] SupportedGrantTypes = ["authorization_code"];
    private static readonly string[] SupportedResponseTypes = ["code"];

    public OAuthDynamicClientRegistrationRequestValidator(IOAuthRequestValidator requestValidator)
    {
        RuleFor(x => x.RedirectUris)
            .NotEmpty()
            .WithMessage("At least one redirect_uri is required.");

        RuleFor(x => x.GrantTypes)
            .Must(grantTypes => grantTypes.Length is 0 || grantTypes.All(SupportedGrantTypes.Contains))
            .WithMessage("Only the authorization_code grant type is supported.");

        RuleFor(x => x.ResponseTypes)
            .Must(responseTypes => responseTypes.Length is 0 || responseTypes.All(SupportedResponseTypes.Contains))
            .WithMessage("Only the code response type is supported.");

        RuleFor(x => x.TokenEndpointAuthMethod)
            .Must(authMethod =>
                string.IsNullOrWhiteSpace(authMethod)
                || string.Equals(authMethod, "none", StringComparison.Ordinal))
            .WithMessage("Only public clients with token_endpoint_auth_method 'none' are supported.");

        RuleForEach(x => x.RedirectUris)
            .Must(requestValidator.IsAllowedRedirectUri)
            .WithMessage("Redirect URIs must use HTTPS or localhost.");
    }
}
