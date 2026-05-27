using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace Kin.KinHub.Shared.Api.Common;

public sealed class JwtAuthenticationMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated is true)
        {
            var sub = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? context.User.FindFirst(ClaimTypes.Email)?.Value;

            if (Guid.TryParse(sub, out var userId)
                && !string.IsNullOrWhiteSpace(email))
            {
                var currentUser = context.RequestServices.GetRequiredService<CurrentUser>();
                currentUser.Populate(new TokenClaims(
                    userId,
                    email,
                    context.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList()));

                var memberIdHeader = context.Request.Headers["X-Member-Id"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(memberIdHeader)
                    && Guid.TryParse(memberIdHeader, out var memberId))
                {
                    currentUser.SetFamilyMemberId(memberId);
                }
            }
        }

        await next(context);
    }
}
