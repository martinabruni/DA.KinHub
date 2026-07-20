using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Functions;

public sealed class KinListBootstrapFunctions(
    IKinListBootstrapService bootstrapService,
    ApiAuthorization authorization,
    KinListTelemetry telemetry)
{
    [Function("KinListBootstrap")]
    public async Task<IActionResult> Bootstrap(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/kinlist/bootstrap")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResults.ApplyCorrelationId(request);
        var startedAt = DateTime.UtcNow;
        using var activity = telemetry.StartActivity("kinlist.bootstrap");

        var authorizationOutcome = await authorization.AuthorizeApiAccessAsync(request.HttpContext);
        if (!authorizationOutcome.Succeeded)
        {
            telemetry.Track("kinlist.bootstrap", authorizationOutcome.Code, DateTime.UtcNow - startedAt);
            return ApiResults.Problem(request, authorizationOutcome.StatusCode, authorizationOutcome.Title, authorizationOutcome.Detail, authorizationOutcome.Code);
        }

        try
        {
            var result = await bootstrapService.GetBootstrapAsync(authorizationOutcome.Request!.ExternalIdentity, cancellationToken);
            telemetry.Track("kinlist.bootstrap", result.State, DateTime.UtcNow - startedAt);
            ApiResults.ApplyNoStore(request.HttpContext.Response);
            return new OkObjectResult(result);
        }
        catch (BusinessAccessDeniedException exception)
        {
            telemetry.Track("kinlist.bootstrap", exception.Code, DateTime.UtcNow - startedAt);
            return ApiResults.Problem(request, StatusCodes.Status403Forbidden, "Forbidden", "Access is not allowed.", exception.Code);
        }
        catch (BusinessDependencyException exception)
        {
            telemetry.Track("kinlist.bootstrap", exception.Code, DateTime.UtcNow - startedAt);
            return ApiResults.Problem(request, StatusCodes.Status503ServiceUnavailable, "Service unavailable", exception.Message, exception.Code);
        }
    }
}
