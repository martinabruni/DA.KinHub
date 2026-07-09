using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kin.KinHub.Identity.Api.Common.Authorization;

namespace Kin.KinHub.Identity.Api.Common.Middlewares;

public sealed class JwtAuthenticationMiddleware : IMiddleware
{
    private readonly IFamilyContextResolver _familyContextResolver;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(
        IFamilyContextResolver familyContextResolver,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _familyContextResolver = familyContextResolver;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var currentUser = context.RequestServices.GetRequiredService<CurrentUser>();

        if (context.User.Identity?.IsAuthenticated is true)
        {
            PopulateCurrentUserFromPrincipal(context, currentUser);
            await TrySetFamilyContextAsync(context, currentUser, context.RequestAborted);
        }

        await next(context);
    }

    /// <summary>
    /// Key under which the family resolution status for the current request is stashed so
    /// the authorization layer can distinguish "no family" (403) from "Core unavailable" (503).
    /// </summary>
    public const string FamilyAccessStatusItemKey = "kinhub.family-access-status";

    private static void PopulateCurrentUserFromPrincipal(HttpContext context, CurrentUser currentUser)
    {
        var sub = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? context.User.FindFirst(ClaimTypes.Email)?.Value;

        if (Guid.TryParse(sub, out var userId)
            && !string.IsNullOrWhiteSpace(email))
        {
            currentUser.Populate(new TokenClaims(
                userId,
                email,
                context.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList(),
                context.User.FindAll("scope")
                    .SelectMany(x => x.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .ToList()));
        }
    }

    private async Task TrySetFamilyContextAsync(HttpContext context, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var familyResult = await _familyContextResolver.ResolveAsync(currentUser.UserId, cancellationToken);
        context.Items[FamilyAccessStatusItemKey] = familyResult.Outcome;

        if (familyResult.Outcome is not FamilyContextOutcome.Success || familyResult.FamilyId is null)
        {
            return;
        }

        // familyId is only ever set on the request-scoped principal from the repository/Core
        // resolution here; it is never read from the JWT, route, or request body.
        currentUser.SetFamilyContext(familyResult.FamilyId.Value);
        _logger.LogDebug("Resolved family context {FamilyId} for user {UserId}.", familyResult.FamilyId, currentUser.UserId);
    }
}
