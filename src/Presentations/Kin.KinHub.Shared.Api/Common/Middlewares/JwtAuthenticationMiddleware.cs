using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Kin.KinHub.Shared.Api.Common;

public sealed class JwtAuthenticationMiddleware : IMiddleware
{
    private readonly IFamilyService _familyService;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(
        IFamilyService familyService,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _familyService = familyService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var currentUser = context.RequestServices.GetRequiredService<CurrentUser>();

        if (context.User.Identity?.IsAuthenticated is true)
        {
            PopulateCurrentUserFromPrincipal(context, currentUser);

            if (!await TrySetFamilyMemberAsync(context, currentUser))
            {
                return;
            }
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

    private async Task<bool> TrySetFamilyMemberAsync(HttpContext context, CurrentUser currentUser)
    {
        var memberIdHeader = context.Request.Headers["X-Member-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(memberIdHeader))
        {
            return true;
        }

        if (!Guid.TryParse(memberIdHeader, out var memberId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "X-Member-Id must be a valid GUID." });
            return false;
        }

        var familyResult = await _familyService.GetFamilyAsync(currentUser.UserId, context.RequestAborted);
        if (!familyResult.IsSuccess || familyResult.Value is null)
        {
            _logger.LogWarning("Rejected X-Member-Id override for user {UserId} because no accessible family was found.", currentUser.UserId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "The authenticated user cannot select a family member for this request." });
            return false;
        }

        if (!familyResult.Value.Members.Any(member => member.Id == memberId))
        {
            _logger.LogWarning("Rejected X-Member-Id override for user {UserId} and member {MemberId}.", currentUser.UserId, memberId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "The supplied X-Member-Id does not belong to the authenticated family." });
            return false;
        }

        currentUser.SetFamilyMemberId(memberId);
        return true;
    }
}
