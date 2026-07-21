using DA.KinHub.Business.Common;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Functions.Configuration;

public sealed class ApiAuthorization(
    IOptions<EntraOptions> options,
    IAuthorizationService authorizationService,
    ExternalIdentityClaimsResolver externalIdentityClaimsResolver)
{
    public const string PolicyName = "ApiAccess";

    public async Task<ApiAuthorizationOutcome> AuthorizeApiAccessAsync(HttpContext context)
    {
        var authentication = await AuthenticateAsync(context);
        if (authentication is not null)
        {
            return authentication;
        }

        var authorization = await authorizationService.AuthorizeAsync(context.User, resource: null, PolicyName);
        if (!authorization.Succeeded)
        {
            return ApiAuthorizationOutcome.Failure(StatusCodes.Status403Forbidden, "Forbidden", "The signed-in user does not have the required API scope.", "auth.scopeRequired");
        }

        if (!externalIdentityClaimsResolver.TryResolve(context.User, out var externalIdentity))
        {
            return ApiAuthorizationOutcome.Failure(StatusCodes.Status401Unauthorized, "Unauthorized", "The token is missing required KinHub identity claims.", "auth.requiredClaims");
        }

        return ApiAuthorizationOutcome.Success(new AuthorizedRequest(context.User, externalIdentity));
    }

    public async Task<ApiAuthorizationOutcome> AuthorizeFamilyAsync(HttpContext context, Guid familyId, CancellationToken cancellationToken)
    {
        var apiAccess = await AuthorizeApiAccessAsync(context);
        if (!apiAccess.Succeeded)
        {
            return apiAccess;
        }

        try
        {
            var authorized = await authorizationService.AuthorizeAsync(
                context.User,
                new FamilyAuthorizationResource(familyId, apiAccess.Request!.ExternalIdentity, cancellationToken),
                FamilyAuthorizationRequirement.PolicyName);

            return authorized.Succeeded
                ? apiAccess
                : ApiAuthorizationOutcome.Failure(StatusCodes.Status403Forbidden, "Forbidden", "Access is not allowed.", "family.accessDenied");
        }
        catch (BusinessDependencyException)
        {
            throw;
        }
    }

    private async Task<ApiAuthorizationOutcome?> AuthenticateAsync(HttpContext context)
    {
        if (!options.Value.Enabled)
        {
            return ApiAuthorizationOutcome.Failure(StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication must be enabled to access KinHub APIs.", "auth.required");
        }

        var authentication = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!authentication.Succeeded || authentication.Principal is null)
        {
            return ApiAuthorizationOutcome.Failure(StatusCodes.Status401Unauthorized, "Unauthorized", "A valid KinHub API token is required.", "auth.required");
        }

        context.User = authentication.Principal;
        return null;
    }
}
