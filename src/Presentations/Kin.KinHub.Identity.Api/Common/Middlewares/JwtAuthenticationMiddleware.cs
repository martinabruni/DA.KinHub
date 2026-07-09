using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Kin.KinHub.Identity.Api.Common.Middlewares;

public sealed class JwtAuthenticationMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var currentUser = context.RequestServices.GetRequiredService<CurrentUser>();

        if (context.User.Identity?.IsAuthenticated is true)
        {
            PopulateCurrentUserFromPrincipal(context, currentUser);
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
}
