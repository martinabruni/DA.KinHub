using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Kin.KinHub.Shared.Api.Common;

public sealed class JwtAuthenticationMiddleware : IMiddleware
{
    private readonly IFamilyOwnershipService _familyOwnershipService;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(
        IFamilyOwnershipService familyOwnershipService,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _familyOwnershipService = familyOwnershipService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var currentUser = context.RequestServices.GetRequiredService<CurrentUser>();

        if (context.User.Identity?.IsAuthenticated is true)
        {
            PopulateCurrentUserFromPrincipal(context, currentUser);
            await TrySetFamilyContextAsync(currentUser, context.RequestAborted);
        }

        await next(context);
    }

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

    private async Task TrySetFamilyContextAsync(CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var familyResult = await _familyOwnershipService.GetCurrentFamilyAsync(currentUser.UserId, cancellationToken);
        if (!familyResult.IsSuccess || familyResult.Family is null)
        {
            return;
        }

        currentUser.SetFamilyContext(familyResult.Family.Id);
        _logger.LogDebug("Resolved family context {FamilyId} for user {UserId}.", familyResult.Family.Id, currentUser.UserId);
    }
}
