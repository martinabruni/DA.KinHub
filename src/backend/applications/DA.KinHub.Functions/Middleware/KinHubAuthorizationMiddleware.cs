using DA.KinHub.Domain.Identity;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Functions.Security;

public sealed class KinHubAuthorizationMiddleware(
    FunctionAccessMetadataProvider metadataProvider,
    ExternalIdentityClaimsResolver externalIdentityClaimsResolver,
    IOptions<EntraOptions> entraOptions,
    KinHubTelemetry telemetry,
    ApiProblemDetailsFactory problemDetailsFactory) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            await next(context);
            return;
        }

        var descriptor = metadataProvider.Get(context.FunctionDefinition);
        if (!descriptor.IsHttp || descriptor.AllowAnonymous)
        {
            await next(context);
            return;
        }

        ApiResults.ApplyNoStorePrivate(httpContext.Response);

        if (!entraOptions.Value.Enabled)
        {
            telemetry.RecordSignal(KinHubOperations.ApiAccess, "auth.required", "authentication");
            ShortCircuit(context, httpContext, StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication must be enabled to access KinHub APIs.", "auth.required");
            return;
        }

        var authenticationService = context.InstanceServices.GetRequiredService<RequestAuthenticationService>();
        var authorizationService = context.InstanceServices.GetRequiredService<IAuthorizationService>();

        var authentication = await authenticationService.AuthenticateAsync(httpContext);
        if (!authentication.Succeeded || authentication.Principal is null)
        {
            telemetry.RecordSignal(KinHubOperations.ApiAccess, "auth.required", "authentication");
            ShortCircuit(context, httpContext, StatusCodes.Status401Unauthorized, "Unauthorized", "A valid KinHub API token is required.", "auth.required");
            return;
        }

        httpContext.User = authentication.Principal;

        var apiAccess = await authorizationService.AuthorizeAsync(httpContext.User, resource: null, SecurityConstants.ApiAccessPolicy);
        if (!apiAccess.Succeeded)
        {
            telemetry.RecordSignal(KinHubOperations.ApiAccess, "auth.scopeRequired", "authorization");
            ShortCircuit(context, httpContext, StatusCodes.Status403Forbidden, "Forbidden", "The signed-in user does not have the required API scope.", "auth.scopeRequired");
            return;
        }

        if (!externalIdentityClaimsResolver.TryResolve(httpContext.User, out var externalIdentity))
        {
            telemetry.RecordSignal(KinHubOperations.ApiAccess, "auth.requiredClaims", "identity");
            ShortCircuit(context, httpContext, StatusCodes.Status401Unauthorized, "Unauthorized", "The token is missing required KinHub identity claims.", "auth.requiredClaims");
            return;
        }

        Guid? familyId = null;
        if (descriptor.RequiresFamilyAccess)
        {
            var familyIdOutcome = TryResolveFamilyId(httpContext.Request, out familyId);
            if (familyIdOutcome is ObjectResult problem)
            {
                var outcome = problem.Value is ProblemDetails details
                    && details.Extensions.TryGetValue(ApiProblemDetailsExtensions.Code, out var code)
                    && code is string problemCode
                    ? problemCode
                    : "family.invalid";
                telemetry.RecordSignal(KinHubOperations.FamilyAuthorization, outcome, "validation");
                context.GetInvocationResult().Value = problem;
                return;
            }

            var authorized = await authorizationService.AuthorizeAsync(
                httpContext.User,
                new FamilyAuthorizationResource(familyId!.Value, externalIdentity, context.CancellationToken),
                SecurityConstants.FamilyPolicy);

            if (!authorized.Succeeded)
            {
                telemetry.RecordSignal(KinHubOperations.FamilyAuthorization, "family.accessDenied", "authorization");
                ShortCircuit(context, httpContext, StatusCodes.Status403Forbidden, "Forbidden", "Access is not allowed.", "family.accessDenied");
                return;
            }
        }

        httpContext.Features.Set(new KinHubAuthorizationFeature(externalIdentity, familyId));
        await next(context);
    }

    private ObjectResult? TryResolveFamilyId(HttpRequest request, out Guid? familyId)
    {
        familyId = null;
        if (!request.Query.TryGetValue(SecurityConstants.FamilyIdQueryParameter, out var values) || values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
        {
            return problemDetailsFactory.Create(request.HttpContext, StatusCodes.Status400BadRequest, "Invalid request", "The familyId query parameter is required.", "family.idRequired");
        }

        if (!Guid.TryParse(values[0], out var parsedFamilyId) || parsedFamilyId == Guid.Empty)
        {
            return problemDetailsFactory.Create(request.HttpContext, StatusCodes.Status400BadRequest, "Invalid request", "The familyId query parameter is invalid.", "family.idInvalid");
        }

        familyId = parsedFamilyId;
        return null;
    }

    private void ShortCircuit(FunctionContext context, HttpContext httpContext, int statusCode, string title, string detail, string code)
    {
        context.GetInvocationResult().Value = problemDetailsFactory.Create(httpContext, statusCode, title, detail, code);
    }
}
