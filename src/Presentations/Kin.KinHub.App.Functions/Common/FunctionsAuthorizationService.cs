using System.Net.Http.Headers;

namespace Kin.KinHub.App.Functions.Common;

public sealed class FunctionsAuthorizationService
{
    private readonly ITokenValidator _tokenValidator;
    private readonly CurrentUser _currentUser;
    private readonly IFamilyContextResolver _familyContextResolver;

    public FunctionsAuthorizationService(
        ITokenValidator tokenValidator,
        CurrentUser currentUser,
        IFamilyContextResolver familyContextResolver)
    {
        _tokenValidator = tokenValidator;
        _currentUser = currentUser;
        _familyContextResolver = familyContextResolver;
    }

    public CurrentUser CurrentUser => _currentUser;

    public Task<IActionResult?> EnsureAuthenticatedAsync(
        HttpRequest request,
        CancellationToken cancellationToken) =>
        EnsureAuthenticatedAsync(
            request,
            requiresFamilyContext: false,
            cancellationToken);

    public Task<IActionResult?> EnsureFamilyContextAsync(
        HttpRequest request,
        CancellationToken cancellationToken) =>
        EnsureAuthenticatedAsync(
            request,
            requiresFamilyContext: true,
            cancellationToken);

    private async Task<IActionResult?> EnsureAuthenticatedAsync(
        HttpRequest request,
        bool requiresFamilyContext,
        CancellationToken cancellationToken)
    {
        var controller = new FunctionController(request.HttpContext);
        var authorizationHeader = request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !AuthenticationHeaderValue.TryParse(authorizationHeader, out var parsedAuthorizationHeader)
            || !string.Equals(parsedAuthorizationHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(parsedAuthorizationHeader.Parameter))
        {
            return ApiProblemDetails.AuthenticationRequired(controller);
        }

        var claims = _tokenValidator.ValidateAccessToken(parsedAuthorizationHeader.Parameter);
        if (claims is null || !claims.Scopes.Contains(OAuthScopes.Read, StringComparer.Ordinal))
        {
            return ApiProblemDetails.AuthenticationRequired(controller);
        }

        _currentUser.Populate(claims);

        if (!requiresFamilyContext)
        {
            return null;
        }

        var familyResolution = await _familyContextResolver.ResolveAsync(_currentUser.UserId, cancellationToken);
        return familyResolution.Outcome switch
        {
            FamilyContextOutcome.Success when familyResolution.FamilyId is not null => SetFamilyContext(familyResolution.FamilyId.Value),
            FamilyContextOutcome.Unavailable => ApiProblemDetails.ServiceUnavailable(
                controller,
                "family_context_unavailable",
                "Family context could not be resolved because Identity is unavailable."),
            _ => ApiProblemDetails.Forbidden(
                controller,
                "family_required",
                "The authenticated user does not currently belong to a family."),
        };
    }

    private IActionResult? SetFamilyContext(Guid familyId)
    {
        _currentUser.SetFamilyContext(familyId);
        return null;
    }
}
