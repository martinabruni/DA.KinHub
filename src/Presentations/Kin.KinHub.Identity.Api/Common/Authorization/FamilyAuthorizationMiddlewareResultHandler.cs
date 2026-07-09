using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Kin.KinHub.Identity.Api.Common.Authorization;

/// <summary>
/// Translates a failed <see cref="FamilyContextRequirement"/> into the appropriate RFC 9457
/// problem detail: 401 when unauthenticated, 503 when the family context could not be resolved
/// because Core is unavailable (fail-closed), and 403 when the user simply has no family.
/// Any other authorization failure falls back to the default handler.
/// </summary>
public sealed class FamilyAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly ICurrentUser _currentUser;

    public FamilyAuthorizationMiddlewareResultHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var isFamilyPolicy = policy.Requirements.OfType<FamilyContextRequirement>().Any();

        if (isFamilyPolicy && !authorizeResult.Succeeded)
        {
            if (!_currentUser.IsAuthenticated)
            {
                await ApiProblemDetails.WriteAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "authentication_required",
                    "Missing or invalid Authorization header.");
                return;
            }

            if (GetFamilyAccessStatus(context) is FamilyContextOutcome.Unavailable)
            {
                await ApiProblemDetails.WriteAsync(
                    context,
                    StatusCodes.Status503ServiceUnavailable,
                    "family_context_unavailable",
                    "Family context could not be resolved because Identity is unavailable.");
                return;
            }

            await ApiProblemDetails.WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                "family_required",
                "The authenticated user does not currently belong to a family.");
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static FamilyContextOutcome? GetFamilyAccessStatus(HttpContext context) =>
        context.Items.TryGetValue(JwtAuthenticationMiddleware.FamilyAccessStatusItemKey, out var value)
            && value is FamilyContextOutcome status
                ? status
                : null;
}
