using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace DA.KinHub.Functions.Security;

public sealed class RequestAuthenticationService
{
    public Task<AuthenticateResult> AuthenticateAsync(HttpContext httpContext)
    {
        return httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
    }
}
