using DA.KinHub.Functions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Functions.Security;

public sealed class ApiScopeAuthorizationHandler(IOptions<EntraOptions> options) : AuthorizationHandler<ApiScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiScopeRequirement requirement)
    {
        var configuredScope = options.Value.Scope;
        var scopes = context.User.Claims
            .Where(claim => claim.Type is SecurityConstants.ScopeClaim or SecurityConstants.LegacyScopeClaim)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (scopes.Contains(configuredScope, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
