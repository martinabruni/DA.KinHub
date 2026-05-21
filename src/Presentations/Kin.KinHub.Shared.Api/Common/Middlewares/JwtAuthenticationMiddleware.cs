
namespace Kin.KinHub.Shared.Api.Common;

public sealed class JwtAuthenticationMiddleware : IMiddleware
{
    private readonly ITokenValidator _tokenValidator;

    public JwtAuthenticationMiddleware(ITokenValidator tokenValidator)
    {
        _tokenValidator = tokenValidator;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authHeader)
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var claims = _tokenValidator.ValidateAccessToken(token);

            if (claims is not null)
            {
                var currentUser = context.RequestServices.GetRequiredService<CurrentUser>();
                currentUser.Populate(claims);

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
