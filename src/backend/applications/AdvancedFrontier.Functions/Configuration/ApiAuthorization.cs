using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AdvancedFrontier.Functions.Configuration;

public sealed class ApiAuthorization(IOptions<EntraOptions> options, IAuthorizationService authorizationService)
{
    public const string PolicyName = "ApiAccess";

    public async Task<bool> AuthorizeAsync(HttpContext context)
    {
        if (!options.Value.Enabled)
        {
            return true;
        }

        var authentication = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!authentication.Succeeded || authentication.Principal is null)
        {
            return false;
        }

        context.User = authentication.Principal;
        return (await authorizationService.AuthorizeAsync(context.User, null, PolicyName)).Succeeded;
    }
}
