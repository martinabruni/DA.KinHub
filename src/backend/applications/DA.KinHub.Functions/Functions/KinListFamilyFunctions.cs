using DA.KinHub.Business.Common;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Functions;

public sealed class KinListFamilyFunctions(ApiAuthorization authorization, KinListTelemetry telemetry)
{
    [Function("KinListFamilyContext")]
    public async Task<IActionResult> FamilyContext(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/kinlist/family-context")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResults.ApplyCorrelationId(request);
        if (!request.Query.TryGetValue("familyId", out var familyIdValues) || string.IsNullOrWhiteSpace(familyIdValues))
        {
            return ApiResults.Problem(request, StatusCodes.Status400BadRequest, "Invalid request", "The familyId query parameter is required.", "family.idRequired");
        }

        if (!Guid.TryParse(familyIdValues.ToString(), out var familyId) || familyId == Guid.Empty)
        {
            return ApiResults.Problem(request, StatusCodes.Status400BadRequest, "Invalid request", "The familyId query parameter is invalid.", "family.idInvalid");
        }

        var startedAt = DateTime.UtcNow;
        using var activity = telemetry.StartActivity("kinlist.family_authorization");
        try
        {
            var authorizationOutcome = await authorization.AuthorizeFamilyAsync(request.HttpContext, familyId, cancellationToken);
            if (!authorizationOutcome.Succeeded)
            {
                telemetry.Track("kinlist.family_authorization", authorizationOutcome.Code, DateTime.UtcNow - startedAt);
                return ApiResults.Problem(request, authorizationOutcome.StatusCode, authorizationOutcome.Title, authorizationOutcome.Detail, authorizationOutcome.Code);
            }

            telemetry.Track("kinlist.family_authorization", "granted", DateTime.UtcNow - startedAt);
            ApiResults.ApplyNoStore(request.HttpContext.Response);
            return new NoContentResult();
        }
        catch (BusinessDependencyException exception)
        {
            telemetry.Track("kinlist.family_authorization", exception.Code, DateTime.UtcNow - startedAt);
            return ApiResults.Problem(request, StatusCodes.Status503ServiceUnavailable, "Service unavailable", exception.Message, exception.Code);
        }
    }
}
